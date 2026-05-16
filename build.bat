dotnet publish "OCR Translator/OCR Translator.csproj" -c Release -o ./dist
del .\dist\*.pdb
rmdir /s /q "OCR Translator\bin"
rmdir /s /q "OCR Translator\obj"