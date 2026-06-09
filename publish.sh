dotnet publish src/SESport.Web/SESport.Web.csproj -c Release -o /opt/sesport
sudo systemctl restart sesport
echo "Published and restarted"
