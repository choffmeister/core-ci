#!/usr/bin/python

import urllib
import zipfile
import os

dependencies = [
	# current version 2.6.2 of nunit / nunit.runners is buggy under mono
	# see https://bugs.launchpad.net/nunitv2/+bug/1076932
	('nuget', 'nunit', 'nunit', '2.6.0.12054'),
	('nuget', 'nunit-runners', 'nunit.runners', '2.6.0.12051'),
	('nuget', 'nlog', 'nlog', '2.0.1.2'),
	('nuget', 'commonservicelocator', 'commonservicelocator', '1.0'),
	('nuget', 'unity', 'unity', '2.1.505.2'),
	('nuget', 'servicestack', 'servicestack', '3.9.55'),
	('nuget', 'servicestack-common', 'servicestack.common', '3.9.55'),
	('nuget', 'servicestack-text', 'servicestack.text', '3.9.55'),
	('nuget', 'servicestack-ormlite-mysql', 'serviceStack.ormlite.mysql', '3.9.55'),
	('nuget', 'mongo-csharp-driver', 'mongocsharpdriver', '1.8.1'),
	('nuget', 'sshnet', 'ssh.net', '2013.4.7'),
	('nuget', 'yamldotnet-core', 'yamldotnet.core', '2.2.0'),
	('nuget', 'yamldotnet-representationmodel', 'yamldotnet.representationmodel', '2.2.0'),
	('github', 'mono-posix', 'choffmeister/mono-posix', 'v4.0.0.0'),
	('github', 'mono-security', 'choffmeister/mono-security', 'v4.0.0.0'),
]

# define paths
script_path = os.path.realpath(__file__)
project_path = os.path.join(os.path.dirname(script_path), '..')
libs_path = os.path.join(project_path, 'src', 'libs')

# download a file via http
def download(url, dest_path):
	urllib.urlretrieve(url, dest_path)

# unpack a zip file to a folder
def unzip(src_path, dest_dir):
	zipfile.ZipFile(src_path).extractall(dest_dir)

# nuget resolver
def resolve_nuget(name, package, version):
	print '- NuGet package %s (%s)' % (package, version)

	nuget_packageurl = 'http://packages.nuget.org/api/v2/package/%s/%s' % (package, version)
	download_path = os.path.join(libs_path, name + '.nupkg')
	unpack_path = os.path.join(libs_path, name)

	download(nuget_packageurl, download_path)
	unzip(download_path, unpack_path)

# github resolver
def resolve_github(name, package, version):
	print '- GitHub package %s (%s)' % (package, version)

	github_packageurl = 'https://github.com/%s/archive/%s.zip' % (package, version)
	download_path = os.path.join(libs_path, name + '.zip')
	unpack_path = os.path.join(libs_path, name)

	download(github_packageurl, download_path)
	unzip(download_path, unpack_path)

# check if libs directory exists
if not os.path.exists(libs_path):
	os.makedirs(libs_path)
else:
	print 'Directory %s already exists. To ensure a clean dependency folder please remove it and restart this script.' % libs_path

for resolver, name, package, version in dependencies:
	if resolver == 'nuget':
		resolve_nuget(name, package, version)
	elif resolver == 'github':
		resolve_github(name, package, version)
	else:
		print '! Unknown resolver %s' % resolver
