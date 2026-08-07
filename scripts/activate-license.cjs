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
    const pred = new Function('el', predText);
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

async function fillEmailAndPassword(page, email, password) {
  // Email step.
  let filled = await fillVisibleInput(page,
    el => (el.id === 'email' || el.name === 'email' || el.type === 'email' || el.autocomplete === 'email'),
    email);
  console.log('[login] email filled: ' + filled);
  await sleep(800);
  await dump(page, '02_email_filled');
  // Click the primary submit/next button.
  await clickPrimaryButton(page, '03_login_email_submitted');

  // Password step (may appear on same or new page).
  for (let i = 0; i < 6; i++) {
    await sleep(2000);
    const pw = await page.$('input[type=password], #password, input[name=password]');
    if (pw) {
      console.log('[login] password field found, attempt ' + i);
      const f = await fillVisibleInput(page, el => el.type === 'password', password);
      console.log('[login] password filled: ' + f);
      await sleep(800);
      await clickPrimaryButton(page, '04_login_password_submitted');
      break;
    }
    if (await page.$('input[name="licenseFile"]')) { console.log('[login] reached license page'); break; }
  }
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
  const client = await page.target().createCDPSession();
  await client.send('Page.setDownloadBehavior', { behavior: 'allow', downloadPath });

  try {
    console.log('[1] goto license.unity3d.com/manual');
    await Promise.all([
      page.goto('https://license.unity3d.com/manual', { waitUntil: 'networkidle0', timeout: 60000 }),
      sleep(3000)
    ]).catch(() => {});
    await dump(page, '01_landed');
    await dismissCookieBanner(page);

    const url = await page.url();
    if (url.includes('login.unity.com') || url.includes('/sign-in')) {
      console.log('[login] on Unity sign-in page, starting login flow');
      await fillEmailAndPassword(page, email, password);
    } else {
      console.log('[login] no login redirect, continuing');
    }

    // Ensure we are on the license page.
    await page.waitForSelector('input[name="licenseFile"]', { timeout: 60000 }).catch(() => {});
    if (!(await page.$('input[name="licenseFile"]'))) {
      console.log('[error] licenseFile input not found after login. Dumping state.');
      await dump(page, '05_no_license_form');
      throw 'licenseFile input not found';
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
      const r = document.querySelector('input[id="type_personal"][value="personal"]');
      if (r) r.click();
    });
    const personal = await page.$('input[id="type_personal"][value="personal"]');
    console.log('[5b] Personal radio clicked: ' + !!personal);

    const cap = await page.$('input[id="option3"][name="personal_capacity"]');
    if (cap) { await page.evaluate(() => document.querySelector('input[id="option3"][name="personal_capacity"]').click()); }

    await dump(page, '08_personal_selected');

    const nextBtn = await page.$('input[class="btn mb10"], input[type=submit], button[type=submit]');
    if (nextBtn) {
      console.log('[6] Clicking next/commit');
      await Promise.all([
        page.click('input[class="btn mb10"], input[type=submit], button[type=submit]').catch(() => {}),
        page.waitForNavigation({ waitUntil: 'load', timeout: 30000 })
      ]).catch(() => {});
      await sleep(3000);
    }
    await dump(page, '09_result');

    let ulf = null;
    for (let i = 0; i < 20; i++) {
      const f = fs.readdirSync(downloadPath).find(f => f.endsWith('.ulf'));
      if (f) { ulf = f; break; }
      await sleep(3000);
    }
    if (ulf) {
      console.log('SUCCESS: downloaded ' + ulf + ' (' + fs.statSync(ulf).size + ' bytes)');
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