# BASED ON https://github.com/mnod/docker-tightvnc/
FROM debian:bookworm

RUN echo locales locales/default_environment_locale select en_US.UTF-8 | debconf-set-selections \
&& echo locales locales/locales_to_be_generated select "en_US.UTF-8 UTF-8" | debconf-set-selections \
&& apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install --no-install-recommends -y \
    locales \
    git \
    vim-tiny \
    less \
    tmux \
    openbox \
    tint2 \
    xfonts-base \
    tightvncserver \
    firefox-esr \
    pcmanfm \
    lxterminal \
    meld \
    scite \
    dbus-x11 \
    ibus-gtk \
    ibus-gtk3 \
    fonts-ipaexfont \
&& apt-get clean \
&& rm -rf /var/lib/apt/lists/*

ENV USER=root

ADD run_vnc.sh /run_vnc.sh
RUN chmod +x /run_vnc.sh

CMD ["/run_vnc.sh"]