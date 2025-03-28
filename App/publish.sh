rm -rf ../Dockerfile/publish
mkdir ../Dockerfile/publish
dotnet publish -c Release -f net9.0 -r linux-x64 --property:PublishDir=../Dockerfile/publish
cp .env ../Dockerfile/publish