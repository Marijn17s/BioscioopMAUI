# UI tests

All tests run on the UITest build to fake the logged in state

## Steps

1. Start Android emulator
2. Build the app via the command below
```bash
dotnet build BioscoopMAUI/BioscoopMAUI.csproj -t:Run -f net10.0-android -c UITest
```
3. Run tests via IDE or using the command below
```bash
dotnet test BioscoopMAUI.UITests/BioscoopMAUI.UITests.csproj
```