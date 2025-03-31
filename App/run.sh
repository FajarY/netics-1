sudo docker pull fajary/netics-1:latest
sudo docker container stop netics-1 2>/dev/null || true
sudo docker container rm netics-1 2>/dev/null || true
# sudo docker container run -d -p 8080:8080 --name netics-1 fajary/netics-1:latest ---> Change On ENV