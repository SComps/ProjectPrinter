#! /bin/bash
rm -rf out
mkdir out
dotnet build /p:EnableWindowsTargeting=true
cp -rv ProjectPrinter/bin/Debug/net9.0/* out/ 
cp -rv device_config/bin/Debug/net9.0/* out/
cd out