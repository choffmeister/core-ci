#!/usr/bin/python

import urllib
import zipfile
import os

dependencies = [
	('nuget', 'nunit', 'nunit', '2.6.2'),
	('nuget', 'servicestack', 'servicestack', '3.9.55'),
	('nuget', 'servicestack-common', 'servicestack.common', '3.9.55'),
	('nuget', 'servicestack-text', 'servicestack.text', '3.9.55'),
	('nuget', 'servicestack-ormlite-mysql', 'serviceStack.ormlite.mysql', '3.9.55'),
]

# define paths
script_path = os.path.realpath(__file__)
project_path = os.path.join(os.path.dirname(script_path), '..')
libs_path = os.path.join(project_path, 'libs')

# download a file via http
def download(url, dest_path):
	urllib.urlretrieve(url, dest_path)

# unpack a zip file to a folder
def unzip(src_path, dest_dir):
	zipfile.ZipFile(src_path).extractall(dest_dir)

# nuget resolver
def resolve_nuget(name, package, version):
	print '- NuGet package %s (%s)' % (package, version)

	nuget_baseurl = 'http://packages.nuget.org/api/v2/package/'
	nuget_packageurl = nuget_baseurl + package + '/' + version
	download_path = os.path.join(libs_path, name + '.nupkg')
	unpack_path = os.path.join(libs_path, name)

	download(nuget_packageurl, download_path)
	unzip(download_path, unpack_path)

# check if libs directory exists
if not os.path.exists(libs_path):
	os.makedirs(libs_path)
else:
	print 'Directory %s already exists. To ensure a clean dependency folder please remove it and restart this script.' % libs_path

for resolver, name, package, version in dependencies:
	if resolver == 'nuget':
		resolve_nuget(name, package, version)
	else:
		print '! Unknown resolver %s' % resolver
