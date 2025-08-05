document.getElementById('setup-host').addEventListener('click', (e) => {
  e.preventDefault();
  chrome.runtime.sendMessage({ action: 'setupNativeHost' });
});

document.getElementById('open-templates').addEventListener('click', (e) => {
  e.preventDefault();
  chrome.runtime.sendMessage({ action: 'openTemplateFolder' });
});
