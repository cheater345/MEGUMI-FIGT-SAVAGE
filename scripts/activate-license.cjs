#!/usr/bin/env node
/* Activate a Unity Personal license by uploading the .alf at license.unity3d.com/manual.
 * The "Personal" option is hidden on the page; the script force-clicks it via JS.
 * Usage:
 *   UNITY_EMAIL=... UNITY_PASSWORD=... node scripts/activate-license.mjs <file.alf>
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
    const txt = await page.evaluate(() => (document.body.innerText || '').replace(/\n{2,}/g, '\n').slice(0, 3000));
    const radios = await page.evaluate(() =>
      [...document.querySelectorAll('input[type=radio], input[type=checkbox], input[type=submit], button')].map(i => ({
        id: i.id, name: i.name, value: i.value, type: i.type, cls: (i.className || '').toString(),
        hidden: !(i.offsetWidth || i.offsetHeight)
      }))
    );
    console.log(`\n===== DUMP[${tag}] url=${await page.url()} =====`);
    console.log(txt);
    console.log('--- interactive elements ---');
    console.log(JSON.stringify(radios, null, 0));
    console.log('=================================\n');
  } catch (e) {
    console.log('dump ' + tag + ' failed: ' + e.message);
  }
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
      sleep(2000)
    ]).catch(() => {});
    await dump(page, '01_landed');

    // If a login form is present, sign in.
    const hasLogin = await page.$('#new_conversations_create_session_form');
    if (hasLogin) {
      console.log('[2] Login form present, signing in...');
      await page.evaluate((t) => { (document.querySelector('input[type=email]')).value = t; }, email);
      await page.evaluate((t) => { (document.querySelector('input[type=password]')).value = t; }, password);
      await Promise.all([
        page.click('input[name="commit"]'),
        page.waitForNavigation({ waitUntil: 'load', timeout: 20000 })
      ]).catch(() => {});
      await dump(page, '02_after_login');
      // Handle possible 2FA + ToS dialogs (best effort, up to a few tries).
      for (let i = 0; i < 8; i++) {
        if (await page.$('button[name="conversations_accept_updated_tos_form[accept]"]')) {
          await page.click('button[name="conversations_accept_updated_tos_form[accept]"]').catch(() => {});
          await dump(page, `03_tos_${i}`);
          break;
        }
        if (await page.$('input[name="conversations_tfa_required_form[verify_code]"]')) {
          console.log('TOTP 2FA requested -- cannot auto-fill without UNITY_TOTP_KEY. Dumping page.');
          await dump(page, `2fa_totp_${i}`);
          throw 'TOTP 2FA required';
        }
        if (await page.$('input[name="conversations_email_tfa_required_form[code]"]')) {
          console.log('Email 2FA requested -- provide EMAIL_PASSWORD to auto-fill. Dumping page.');
          await dump(page, `2fa_email_${i}`);
          throw 'Email 2FA required';
        }
        if (await page.$('input[name="licenseFile"]')) break;
        await sleep(2000);
      }
    }

    // Upload the alf file.
    console.log('[3] Uploading .alf');
    await page.waitForSelector('input[name="licenseFile"]', { timeout: 30000 });
    await page.$eval('input[name="licenseFile"]', (el, p) => { el.files = undefined; }, alf);
    const input = await page.$('input[name="licenseFile"]');
    await input.uploadFile(path.resolve(alf));
    await dump(page, '02_alf_selected');
    console.log('[4] Clicking commit to send the request file');
    await Promise.all([
      page.click('input[name="commit"]'),
      page.waitForNavigation({ waitUntil: 'load', timeout: 30000 })
    ]).catch(() => {});
    await sleep(3000);
    await dump(page, '03_after_commit');

    // License configuration page: reveal the hidden "Personal" option and select it.
    console.log('[5] Looking for the hidden Personal option');
    const result = await page.evaluate(() => {
      const opts = [...document.querySelectorAll('.option, .option-item, li, label')]
        .filter(n => /personal/i.test(n.textContent || ''))
        .map(n => ({ tag: n.tagName, cls: (n.className || '').toString(), html: n.outerHTML.slice(0, 300) }));
      const hidden = [...document.querySelectorAll('[style*="display: none"]')].length;
      const typeRadios = [...document.querySelectorAll('input[type=radio]')].map(i => ({ id: i.id, name: i.name, value: i.value }));
      return { opts, hiddenCount: hidden, typeRadios };
    });
    console.log('personal-ish elements: ' + JSON.stringify(result, null, 1));

    // Unhide the personal panel if present.
    await page.evaluate(() => {
      const n = document.querySelector('.option-personal, [class*="option-personal"]');
      if (n) n.removeAttribute('style');
    });
    // Click the Personal radio (regardless of visibility).
    const personal = await page.$('input[id="type_personal"][value="personal"]');
    if (personal) {
      await page.evaluate(() => document.querySelector('input[id="type_personal"][value="personal"]').click());
      console.log('[5b] Clicked Personal radio');
    } else {
      console.log('[5b] Personal radio NOT FOUND on this page');
    }
    const cap = await page.$('input[id="option3"][name="personal_capacity"]');
    if (cap) {
      await page.evaluate(() => document.querySelector('input[id="option3"][name="personal_capacity"]').click());
    }
    await dump(page, '04_personal_selected');

    const nextBtn = await page.$('input[class="btn mb10"], input[type=submit]');
    if (nextBtn) {
      console.log('[6] Clicking next/commit');
      await Promise.all([
        page.click('input[class="btn mb10"], input[type=submit]').then(p => p).catch(() => {}),
        page.waitForNavigation({ waitUntil: 'load', timeout: 30000 })
      ]).catch(() => {});
      await sleep(3000);
    }
    await dump(page, '05_result');

    // Wait for .ulf download.
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