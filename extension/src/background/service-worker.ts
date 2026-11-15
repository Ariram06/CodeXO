// Codexo Service Worker (Background Script)
// Placeholder — real messaging logic will be added in M9

// Open side panel when the extension action button is clicked
chrome.action.onClicked.addListener((tab) => {
  if (tab.id) {
    chrome.sidePanel.open({ tabId: tab.id });
  }
});

console.log("[Codexo] Service worker loaded");
