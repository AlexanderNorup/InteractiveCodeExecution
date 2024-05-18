#! /bin/bash
# BASED ON https://github.com/mnod/docker-tightvnc/
vncdir=/root/.vnc
vncpassfile=${vncdir}/passwd

if [ ! -d ${vncdir} ]; then
  echo "# making ${vncdir}..."
  mkdir ${vncdir}
  chmod 700 ${vncdir}
fi

if [ ! -f ${vncpassfile} ]; then
  echo "# making ${vncpassfile}..."

  PASS=${VNC_PASSWORD-"12345678"}
  echo ${PASS} | vncpasswd -f > ${vncpassfile}
  chmod 600 ${vncpassfile}
fi

echo "# starting vncserver processes..."
DESKTOP_SIZE=${RESOLUTION-"854x480"}
vncserver -geometry ${DESKTOP_SIZE} :0

sleep 10
tint2 & # Starts a task-bar application

sleep infinity