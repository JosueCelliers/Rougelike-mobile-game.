// Browser glue for the web build. Unity keeps persistentDataPath in an in-memory file
// system backed by IndexedDB; this flushes it so a saved run survives a page reload.
mergeInto(LibraryManager.library, {
  BadLie_SyncFS: function () {
    try {
      FS.syncfs(false, function (err) {
        if (err) console.warn("BAD LIE: could not persist the save", err);
      });
    } catch (e) {
      console.warn("BAD LIE: save sync unavailable", e);
    }
  }
});
