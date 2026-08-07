#!/usr/bin/env node
/* Activate a Unity Personal license by uploading the .alf at license.unity3d.com/manual.
 * Handles the current flow: optional login redirect (login.unity.com), cookie banner,
 * hidden "Personal" option (force-clicked via JS), then downloads the .ulf file.
 * Usage:
 *   UNITY_EMAIL=... UNITY_PASSWORD=... node scripts/activate-license.cjs <file.alf>
 * Requires a Chrome binary (default: google-chrome / chromium) or PUPPETEER_EXECUTABLE_PATH.
 */
const fs = require('fs');
const path = require('path');
const puppeteer = require('puppeteer-core');

const alf = process.argv[2];
const email = process.env.UNITY_EMAIL;
const password = process.env.UNITY_PASSWORD;
const chromePath = process.env.PUPPETEER_EXECUTABLE_PATH ||
  ['/usr/bin/google-chrome', '/usr/bin/google-chrome-stable', '/usr/bin/chromium', '/usr/bin/chromium-browser']
    .find(p => fs.existsSync(p));

if (!alf || !fs.existsSync(alf)) { console.error('[ERROR] missing .alf file: ' + alf); process.exit(1); }
if (!email || !password) { console.error('[ERROR] UNITY_EMAIL/UNITY_PASSWORD env not set'); process.exit(1); }
if (!chromePath) { console.error('[ERROR] no Chrome/Chromium binary found'); process.exit(1); }

const sleep = ms => new Promise(r => setTimeout(r, ms));

async function dump(page, tag) {
  try {
    const html = await page.evaluate(() => document.documentElement.outerHTML);
    fs.writeFileSync(`page_${tag}.html`, html);
    await page.screenshot({ path: `page_${tag}.png`, fullPage: true });
    const txt = await page.evaluate(() => (document.body.innerText || '').replace(/\n{2,}/g, '\n').slice(0, 2500));
    console.log(`\n===== DUMP[${tag}] url=${await page.url()} =====`);
    console.log(txt);
    console.log('=================================\n');
  } catch (e) {
    console.log('dump ' + tag + ' failed: ' + e.message);
  }
}

async function dismissCookieBanner(page) {
  const sel = '#onetrust-accept-btn-handler, .accept-recommended-btn-handler, #onetrust-reject-all-handler';
  const el = await page.$(sel);
  if (el) { await el.click().catch(() => {}); await sleep(1500); console.log('[cookie] banner dismissed'); }
}

async function fillVisibleInput(page, predicate, value) {
  return page.evaluate((predText, v) => {
    const pred = new Function('el', 'return ' + predText);
    const inputs = [...document.querySelectorAll('input')];
    const el = inputs.find(i => pred(i));
    if (!el) return false;
    el.focus();
    const proto = el.tagName === 'TEXTAREA' ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
    const setter = Object.getOwnPropertyDescriptor(proto, 'value').set;
    setter.call(el, v);
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
  }, predicate.toString(), value);
}

async function typeInto(page, selector, value) {
  await page.waitForSelector(selector, { timeout: 20000 });
  const el = await page.$(selector);
  await el.click({ clickCount: 3 }).catch(() => {});
  await el.type(value, { delay: 30 });
}

async function fillEmailAndPassword(page, email, password) {
  await typeInto(page, '#email, input[name="email"]', email);
  console.log('[login] email typed');
  await dump(page, '02_email_filled');
  // Submit the email step by clicking the "Continue" button.
  await clickTextButton(page, /continue|next/i, false);
  await sleep(3500);
  await dump(page, '03_login_email_submitted');

  // Password step.
  for (let i = 0; i < 8; i++) {
    if (await page.$('#password, input[type="password"]')) { break; }
    await sleep(2000);
  }
  if (await page.$('#password, input[type="password"]')) {
    await typeInto(page, '#password, input[type="password"]', password);
    console.log('[login] password typed');
    await sleep(800);
    // Press Enter and tolerate the navigation it triggers (login success).
    await page.keyboard.press('Enter').catch(() => {});
    try {
      await Promise.race([
        page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }),
        sleep(4000),
      ]).catch(() => {});
    } catch {}
    await sleep(3000);
    // If the password field is still present it means we are still on the
    // sign-in page, so click the "Sign in" button explicitly.
    let stillPassword;
    try { stillPassword = !!(await page.$('#password, input[type="password"]')); }
    catch { stillPassword = false; }
    if (stillPassword) {
      await clickTextButton(page, 'sign in', true);
      await sleep(3500);
    } else {
      console.log('[login] password submitted, navigation in progress');
    }
  } else {
    // Maybe it is showing 2FA or error page instead.
    const txt = await page.evaluate(() => (document.body.innerText || '').slice(0, 1500));
    console.log('[login] no password field found. Page text:\n' + txt);
  }
  await dump(page, '04_after_login');
}

async function clickTextButton(page, pattern, exact) {
  const src = (pattern instanceof RegExp) ? pattern.source : String(pattern);
  const isExact = !!exact;
  const clicked = await page.evaluate((patSrc, isExact) => {
    const re = new RegExp(patSrc, 'i');
    const holders = [...document.querySelectorAll('button, input[type=submit], input[type=button], [role=button], a')];
    const match = holders.find(b => {
      const t = (b.innerText || b.value || '').trim();
      if (!t) return false;
      return isExact ? (re.test(t) && t.length <= 60) : re.test(t);
    });
    if (!match) return 'none';
    match.click();
    return (match.innerText || match.value || '').trim();
  }, src, isExact);
  console.log('[click]' + src + ' -> ' + clicked);
  await sleep(3000);
  return clicked !== 'none';
}

async function clickPrimaryButton(page, dumpTag) {
  const clicked = await page.evaluate(() => {
    const buttons = [...document.querySelectorAll('button, input[type=submit], input[type=button], a')];
    const candidates = buttons.filter(b => {
      const t = (b.innerText || b.value || '').trim().toLowerCase();
      if (!t) return false;
      const skip = ['cookie', 'accept', 'reject', 'manage', 'clear', 'filter', 'cancel', 'language', 'search'];
      if (skip.some(s => t.includes(s))) return false;
      return t.includes('continue') || t.includes('next') || t.includes('sign in') || t.includes('log in') ||
             t.includes('submit') || t.includes('proceed') || t.includes('agree') || t.includes('enter');
    });
    if (!candidates.length) return 'none';
    const btn = candidates[candidates.length - 1];
    btn.click();
    return btn.innerText || btn.value || btn.tagName;
  });
  console.log('[click] primary button: ' + clicked);
  await sleep(2500);
  if (dumpTag) await dump(page, dumpTag);
  return clicked !== 'none';
}

(async () => {
  const browser = await puppeteer.launch({
    executablePath: chromePath,
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage']
  });
  const page = await browser.newPage();
  const downloadPath = process.cwd();
  const expectedUlf = path.join(downloadPath, 'Unity_lic.ulf');
  // Listen for downloads via Puppeteer's events (more reliable than CDP setDownloadBehavior).
  page.on('download', async d => {
    console.log('[download] event received: ' + d.suggestedFilename());
    try {
      await d.saveAs(expectedUlf);
      console.log('[download] saved to ' + expectedUlf);
    } catch (e) {
      console.log('[download] saveAs failed: ' + e.message);
    }
  });

  try {
    // Go to sign-up page directly: /en/sign-in does a server-side redirect loop
    // (ERR_TOO_MANY_REDIRECTS) but /en/sign-up loads fine, and the same page
    // exposes a same-origin "Sign in" toggle that switches to the login SPA view
    // via client-side routing (no server round-trip).
    let url;
    const startUrl = 'https://login.unity.com/en/sign-up';
    for (let attempt = 0; attempt < 4; attempt++) {
      if (attempt > 0) {
        console.log('[goto] retry attempt ' + attempt + ', clearing cookies');
        await page.evaluate(() => localStorage.clear()).catch(() => {});
        await sleep(3000);
      }
      await Promise.all([
        page.goto(startUrl, { waitUntil: 'domcontentloaded', timeout: 45000 })
          .catch(e => console.log('[goto] navigation error (attempt ' + attempt + '): ' + e.message)),
        sleep(2500)
      ]);
      url = await page.url();
      console.log('[goto] attempt ' + attempt + ' url=' + url);
      if (!url.includes('chromewebdata') && !url.startsWith('chrome-error')) break;
    }
    await sleep(2000);
    await dump(page, '01_landed');
    await dismissCookieBanner(page);

    url = await page.url();
    if (url.includes('/sign-up') || await page.$('a[href*="sign-in"]')) {
      console.log('[login] landing on sign-up page; clicking the Sign in link');
      const clicked = await page.evaluate(() => {
        const a = [...document.querySelectorAll('a')].find(x => /sign in/i.test(x.textContent || '') && /sign-in/i.test(x.href || ''));
        if (a) { a.click(); return a.href; }
        return 'none';
      });
      console.log('[login] sign-in link clicked: ' + clicked);
      await sleep(3500);
      await dump(page, '01b_after_signin_toggle');
      await fillEmailAndPassword(page, email, password);
    } else if (url.includes('/sign-in') || url.includes('login.unity.com')) {
      console.log('[login] on Unity sign-in page, starting login flow');
      await fillEmailAndPassword(page, email, password);
    } else {
      console.log('[login] no login redirect, continuing');
    }

    // Ensure we are on the license page.
    if (!(await page.$('input[name="licenseFile"]'))) {
      // After login Unity lands somewhere generic; go to the manual page now.
      console.log('[nav] not on license form, navigating to /manual');
      await Promise.all([
        page.goto('https://license.unity3d.com/manual', { waitUntil: 'domcontentloaded', timeout: 45000 })
          .catch(e => console.log('[nav] error: ' + e.message)),
        sleep(3500)
      ]);
      await dump(page, '05_after_nav');
    }
    await page.waitForSelector('input[name="licenseFile"]', { timeout: 60000 }).catch(() => {});
    if (!(await page.$('input[name="licenseFile"]'))) {
      console.log('[error] licenseFile input not found after login. Dumping state.');
      await dump(page, '05_no_license_form');
      // Retry the manual navigation a couple of times (redirect loops are flaky).
      for (let attempt = 0; attempt < 3 && !(await page.$('input[name="licenseFile"]')); attempt++) {
        await page.goto('https://license.unity3d.com/manual', { waitUntil: 'domcontentloaded', timeout: 45000 })
          .catch(() => {});
        await sleep(4000);
        console.log('[nav] retry ' + attempt + ' url=' + await page.url());
      }
      if (!(await page.$('input[name="licenseFile"]'))) {
        await dump(page, '05_no_license_form');
        throw 'licenseFile input not found';
      }
    }

    console.log('[3] Uploading .alf');
    const input = await page.$('input[name="licenseFile"]');
    await input.uploadFile(path.resolve(alf));
    await dump(page, '06_alf_selected');
    console.log('[4] Clicking commit to send the request file');
    await Promise.all([
      page.click('input[name="commit"]'),
      page.waitForNavigation({ waitUntil: 'load', timeout: 30000 })
    ]).catch(() => {});
    await sleep(3000);
    await dump(page, '07_after_commit');

    console.log('[5] Looking for the hidden Personal option');
    const result = await page.evaluate(() => {
      const opts = [...document.querySelectorAll('.option, .option-item, li, label, div')]
        .filter(n => /personal/i.test(n.textContent || '') && n.textContent.length < 120)
        .map(n => ({ tag: n.tagName, cls: (n.className || '').toString(), txt: (n.textContent || '').slice(0, 60) }));
      const typeRadios = [...document.querySelectorAll('input[type=radio]')].map(i => ({ id: i.id, name: i.name, value: i.value }));
      const hiddenPersonal = [...document.querySelectorAll('[class*="option-personal"]')].map(n => n.outerHTML.slice(0, 200));
      return { opts: opts.slice(0, 10), typeRadios, hiddenPersonal };
    });
    console.log('personal-ish elements: ' + JSON.stringify(result, null, 1));

    // Unhide the personal panel if present, then click the Personal radio regardless of visibility.
    await page.evaluate(() => {
      const n = document.querySelector('.option-personal, [class*="option-personal"]');
      if (n) n.removeAttribute('style');
    });
    const personal = await page.$('input[id="type_personal"][value="personal"]');
    if (personal) {
      await page.evaluate(() => document.querySelector('input[id="type_personal"][value="personal"]').click());
      console.log('[5b] Personal radio clicked');
    } else {
      console.log('[5b] Personal radio NOT FOUND on this page');
    }
    // Personal capacity: pick "I don't use Unity in a professional capacity".
    const cap = await page.$('input[id="option3"][name="personal_capacity"]');
    if (cap) {
      await page.evaluate(() => document.querySelector('input[id="option3"][name="personal_capacity"]').click());
      console.log('[5c] Personal capacity clicked (option3)');
    }

    await dump(page, '08_personal_selected');

    // Verify both radios are actually checked before submitting.
    const checked = await page.evaluate(() => ({
      personal: document.querySelector('input[id="type_personal"][value="personal"]')?.checked,
      capacity: document.querySelector('input[name="personal_capacity"]:checked')?.id || null
    }));
    console.log('[6] checked state: ' + JSON.stringify(checked));

    // Submit ONLY the Personal section's "Next" button (the page also has a serial Next).
    const nextBtn = await page.$('input[class="btn mb10"]');
    if (nextBtn && checked.personal && checked.capacity) {
      console.log('[7] Clicking Personal section Next (btn mb10)');
      await Promise.all([
        page.click('input[class="btn mb10"]').catch(() => {}),
        page.waitForNavigation({ waitUntil: 'load', timeout: 30000 })
      ]).catch(() => {});
      await sleep(3000);
    } else {
      console.log('[7] Skipping submit: personal=' + checked.personal + ' capacity=' + checked.capacity);
    }
    await dump(page, '09_result');

    // If a "Download license file" step appears, the download is handled via AJAX
    // POST /genesis/activation/download-license which returns the license XML.
    let ulf = null;
    if (await page.$('input[name="commit"][value*="ownload"]')) {
      console.log('[8] Download page detected, fetching license XML via download-license endpoint');
      await dump(page, '10_download');
      const xml = await page.evaluate(async () => {
        const res = await fetch('/genesis/activation/download-license', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{}' });
        if (!res.ok) return { ok: false, status: res.status, text: (await res.text()).slice(0, 300) };
        const json = await res.json();
        if (json && json.data && json.data.xml) return { ok: true, xml: json.data.xml };
        return { ok: false, status: res.status, text: JSON.stringify(json).slice(0, 300) };
      });
      console.log('[8] AJAX license response: ' + JSON.stringify(xml).slice(0, 200));
      if (xml.ok) {
        fs.writeFileSync(expectedUlf, xml.xml);
        if (fs.existsSync(expectedUlf) && fs.statSync(expectedUlf).size > 0) { ulf = expectedUlf; }
      }
    } else {
      console.log('[8] No obvious download page.');
    }
    if (!ulf) {
      // Fallback: scan workspace and download dir for any .ulf
      for (let i = 0; i < 20; i++) {
        const f = fs.readdirSync(downloadPath).find(f => f.endsWith('.ulf'));
        if (f) { ulf = path.join(downloadPath, f); break; }
        await sleep(3000);
      }
    }
    await dump(page, '10_download');
    if (ulf) {
      console.log('SUCCESS: downloaded ' + ulf + ' (' + fs.statSync(ulf).size + ' bytes)');
      console.log('CONTENT_HEAD ' + fs.readFileSync(ulf, 'utf8').slice(0, 80).replace(/\n/g, ' '));
    } else {
      console.log('NO .ulf downloaded. See dump files and screenshot.');
    }
    await browser.close();
    process.exit(ulf ? 0 : 1);
  } catch (err) {
    console.log('FATAL: ' + (err && err.message ? err.message : err));
    try {
      await page.screenshot({ path: 'page_error.png', fullPage: true });
      const html = await page.evaluate(() => document.documentElement.outerHTML);
      fs.writeFileSync('page_error.html', html);
    } catch (_) {}
    await browser.close().catch(() => {});
    process.exit(1);
  }
})();