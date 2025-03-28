FROM ubuntu:22.04

WORKDIR /var/www/neticscicd

RUN apt update -y
RUN apt install libicu70 -y

COPY ./publish/* .
EXPOSE 8080

ENTRYPOINT ["./App"]