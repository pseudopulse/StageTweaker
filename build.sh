rm -rf StageTweaker/bin
dotnet restore
dotnet build
rm -rf ~/.config/r2modmanPlus-local/RiskOfRain2/profiles/StageTweaker/BepInEx/plugins/StageTweaker
cp -r StageTweaker/bin/Debug/netstandard2.0  ~/.config/r2modmanPlus-local/RiskOfRain2/profiles/StageTweaker/BepInEx/plugins/StageTweaker

rm -rf STBuild
mkdir STBuild
cp icon.png STBuild/
cp manifest.json STBuild/
cp README.md STBuild/
cp StageTweaker/bin/Debug/netstandard2.0/StageTweaker.dll STBuild/
cd STBuild
rm ../ST.zip
zip ../ST.zip *
cd ..