// File: Assets/Plugins/DeviceDetectorPlugin.jslib

var DeviceDetectorPlugin = {
  DetectDevice: function () {
    var userAgent = navigator.userAgent || navigator.vendor || window.opera;
    
    // Check for iPad specifically
    if (/iPad/i.test(userAgent)) {
      return allocateUTF8("iPad");
    }
    
    // Check for other iOS devices
    if (/iPhone|iPod/.test(userAgent) && !window.MSStream) {
      return allocateUTF8("iOS");
    }
    
    // Check for Android
    if (/android/i.test(userAgent)) {
      return allocateUTF8("Android");
    }
    
    // If no specific mobile match, return PC as default
    return allocateUTF8("PC");
  },
  
  // Helper function to allocate UTF8 string
  allocateUTF8: function (str) {
    var bufferSize = lengthBytesUTF8(str) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(str, buffer, bufferSize);
    return buffer;
  }
};

mergeInto(LibraryManager.library, DeviceDetectorPlugin);