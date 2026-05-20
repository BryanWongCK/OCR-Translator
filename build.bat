dotnet publish "OCR Translator/OCR Translator.csproj" -c Release -o .\dist\Release
del /q .\dist\Release\*.pdb

dotnet publish "OCR Translator/OCR Translator.csproj" -c "Release Rapid" -o ".\dist\Release Rapid"
del /q ".\dist\Release Rapid\*.pdb" 2>nul

rmdir /s /q ".\dist\Release Rapid\runtimes\android" 2>nul
rmdir /s /q ".\dist\Release Rapid\runtimes\ios" 2>nul
rmdir /s /q ".\dist\Release Rapid\runtimes\linux-arm64" 2>nul
rmdir /s /q ".\dist\Release Rapid\runtimes\linux-x64" 2>nul
rmdir /s /q ".\dist\Release Rapid\runtimes\osx-arm64" 2>nul
rmdir /s /q ".\dist\Release Rapid\runtimes\win-arm64" 2>nul
rmdir /s /q ".\dist\Release Rapid\runtimes\win-x86" 2>nul