# electron-promote-windows-tray-items

Promotes Windows tray items created by the app from the toolbar customization area to the toolbar itself.

May be safely required on non-Windows platforms, though the package will return an error if it is attempted to be used on non-Windows platforms.

This project is currently compatible with Electron 0.36.2 due to its dependence on
[electron-edge](https://github.com/kexplo/electron-edge#electron-edge).

## Installation

For Electron:

```js
npm install electron-promote-windows-tray-items --save
```

## Usage

```js
var promoteWindowsTrayItems = require('electron-promote-windows-tray-items');

// Create a tray item.
var icon = new Tray(/* ... */);

// Icon will now be in the toolbar customization area without the user explicitly toggling it to show in the toolbar.

if (process.platform === 'win32') {
  promoteWindowsTrayItems(function(err) {
    // Icon will now be in the toolbar itself unless the user explicitly hid it from the toolbar.
  });
}
```

## Contributing

We welcome pull requests! Please lint your code.

## Release History

* 1.0.0 Initial release.

## License

// TODO(jeff): Fill this out and acknowledge `Squirrel.Windows`.
