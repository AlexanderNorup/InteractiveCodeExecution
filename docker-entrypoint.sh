#!/bin/sh
set -e
DOCKER_SOCKET=/var/run/docker.sock
RUNUSER=app

if [ -S ${DOCKER_SOCKET} ]; then
    chmod 666 $DOCKER_SOCKET
fi
exec runuser -u $RUNUSER -- $@