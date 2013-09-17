# core:ci

## Installation server (Ubuntu 12.04 LTS x64)

```bash
$ sudo apt-get update
# install git
$ sudo apt-get install -y git
# install mono
$ sudo apt-get install -y mono-devel mono-gmcs nunit-console
# install mongodb
$ sudo apt-get install -y mongo
# install nodejs, grunt and bower
$ sudo apt-get install -y python-software-properties
$ sudo add-apt-repository -y ppa:chris-lea/node.js
$ sudo apt-get update
$ sudo apt-get install -y nodejs
$ sudo npm install -g grunt
$ sudo npm install -g bower

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
# get webapp dependencies
$ cd web
$ sudo -u coreci -H npm install
$ sudo -u coreci -H bower install
$ cd ..
# compile
$ sudo -u coreci -H ./tools/build.py all
# register and start as daemon
$ sudo cp docs/upstart/coreci-server.conf /etc/init/
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
$ wget http://files.vagrantup.com/packages/b12c7e8814171c1295ef82416ffe51e8a168a244/vagrant_1.3.1_x86_64.deb
$ sudo dpkg -i vagrant_1.3.1_x86_64.deb

# create core:ci user
$ sudo adduser coreci --disabled-login --gecos ""
$ cd /home/coreci
# import StartCom root certificate (only for core:ci user)
$ sudo -u coreci -H certmgr -add -c Trust docs/startcom-ca.crt
# get sources
$ sudo -u coreci -H git clone git@github.com:choffmeister/core-ci.git
$ cd /home/coreci/core-ci
# get dependencies
$ sudo -u coreci -H ./tools/dependencies.py
# compile
$ sudo -u coreci -H ./tools/build.py sources
# register and start as daemon
$ sudo cp docs/upstart/coreci-worker.conf /etc/init/
$ sudo start coreci-worker
```
