# SESport Web Wrapper

Minimal Android app that opens `https://sesport.se` in a WebView.

## What it does

- Loads `https://sesport.se` on startup.
- Blocks navigation outside `sesport.se`.
- Uses the device back button to navigate WebView history.

## Open in Android Studio

Open `android/SESportWebWrapper` as a project in Android Studio.
Android Studio will sync the Gradle project and let you run it on an
emulator or device.

## Build from CLI

Use the Gradle wrapper in the project root:

```bash
cd android/SESportWebWrapper
./gradlew assembleDebug
```

## Release signing

Copy `keystore.properties.example` to `keystore.properties` and fill in
the values. The release build will then sign the app with that keystore.

Required keys:

- `storeFile`
- `storePassword`
- `keyAlias`
- `keyPassword`

You can also generate both files with:

```bash
cd android/SESportWebWrapper
./bin/create-release-keystore.sh /absolute/path/to/release-keystore.jks
```

That script will prompt for the passwords and write
`keystore.properties` for you.

## Notes

- The project is intentionally tiny.
- It needs the Android SDK and Java 17 to build.
