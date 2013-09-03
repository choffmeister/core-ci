# core:ci

## Installation server (Ubuntu 12.04 LTS x64)

```bash
$ sudo apt-get update
# install git
$ sudo apt-get install -y git
# install mono
$ sudo apt-get install -y mono-devel mono-gmcs nunit-console
# create core:ci user
$ sudo adduser coreci --disabled-login --gecos ""
$ cd /home/coreci
# import root certificates (only for core:ci user)
$ sudo -u coreci -H mozroots --import
# get sources
$ sudo -u coreci -H git clone git@github.com:choffmeister/core-ci.git
$ cd /home/coreci/core-ci
# get dependencies
$ sudo -u coreci -H ./tools/dependencies.py
# compile
$ sudo -u coreci -H xbuild /p:Configuration=Release
# register and start as daemon
$ sudo cp scripts/coreci-server.conf /etc/init/
$ sudo start coreci-server
```

## Installation worker (Ubuntu 12.04 LTS x64)

```bash
$ sudo apt-get update
# install git
$ sudo apt-get install -y git
# install mono
$ sudo apt-get install -y mono-devel mono-gmcs nunit-console
# install virtualbox
$ sudo apt-get install -y virtualbox
# install vagrant
$ wget http://files.vagrantup.com/packages/7ec0ee1d00a916f80b109a298bab08e391945243/vagrant_1.2.7_x86_64.deb
$ sudo dpkg -i vagrant_1.2.7_x86_64.deb
# create core:ci user
$ sudo adduser coreci --disabled-login --gecos ""
$ cd /home/coreci
# import root certificates (only for core:ci user)
$ sudo -u coreci -H mozroots --import
# get sources
$ sudo -u coreci -H git clone git@github.com:choffmeister/core-ci.git
$ cd /home/coreci/core-ci
# get dependencies
$ sudo -u coreci -H ./tools/dependencies.py
# compile
$ sudo -u coreci -H xbuild /p:Configuration=Release
# register and start as daemon
$ sudo cp scripts/coreci-worker.conf /etc/init/
$ sudo start coreci-worker
```
