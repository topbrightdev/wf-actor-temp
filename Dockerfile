FROM mcr.microsoft.com/dotnet/runtime-deps:6.0

# RUN apk add -U --no-cache ca-certificates
RUN echo "hosts: files dns" > /etc/nsswitch.conf


# Need this stuff uncommented for prod but will remove the ability to SSH into container
# FROM scratch

# WORKDIR /
# COPY --from=alpine /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/
# COPY --from=alpine /etc/nsswitch.conf /etc/nsswitch.conf

WORKDIR /usr/src/app
COPY ./lib/bin/linux/amd64 .
COPY ./ms_standalone/bin/Release/netcoreapp3.1/linux-x64/publish/Timeplay.WheelOfFortune.Actor.Mothership ./wheeloffortune/mothership
COPY ./log4net.config ./log4net.config
COPY ./sat_standalone/bin/Release/netcoreapp3.1/linux-x64/publish/Timeplay.WheelOfFortune.Actor.Satellite ./wheeloffortune/satellite
# COPY ./rmb ./rmb

EXPOSE 8080
EXPOSE 8081

CMD ["./rmb", "-log", "direct"]
