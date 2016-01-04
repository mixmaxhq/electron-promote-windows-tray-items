var IS_WIN = process.platform === 'win32';

var app = require('electron').app;
// `electron-edge` will fail to load on non-Windows platforms.
var edge = IS_WIN ? require('electron-edge') : null;
var path = require('path');

/**
 * Promotes all tray items created by the specified EXE from the toolbar customization area
 * to the toolbar itself if the user has not explicitly specified that they should never be
 * shown in the toolbar.
 *
 * @param {function<Error>} done - The callback to invoke when promotion has succeeded or failed.
 */
var promoteWindowsTrayItems = function(done) {
  if (process.platform !== 'win32') {
    process.nextTick(function() {
      done(new Error('promoteWindowsTrayItems is not available on non-Windows platforms.'));
    });
    return;
  }

  var promoteWindowsTrayItemsImpl = edge.func(path.join(__dirname, 'promoteTrayItems.cs'));

  var exeName = path.basename(app.getPath('exe'));
  promoteWindowsTrayItemsImpl(exeName, done);
};

module.exports = promoteWindowsTrayItems;
