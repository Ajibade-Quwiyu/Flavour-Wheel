mergeInto(LibraryManager.library, {
  DownloadFile: function(fileName, data) {
    var link = document.createElement('a');
    link.download = UTF8ToString(fileName);
    link.href = "data:text/csv;base64," + UTF8ToString(data);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
});