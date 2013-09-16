#!/usr/bin/python

import os
import subprocess
import distutils.core

# configuration
src_config = 'Release'
web_config = 'prod'

# retrieve project base path
script_path = os.path.realpath(__file__)
project_path = os.path.join(os.path.dirname(script_path), '..')
target_path = os.path.join(project_path, 'target')

def remove_folder(path):
	if os.path.exists(path):
		for root, dirs, files in os.walk(path, topdown=False):
			for name in files:
				os.remove(os.path.join(root, name))
			for name in dirs:
				os.rmdir(os.path.join(root, name))
		os.rmdir(path)

def ensure_folder_exists(path):
	if not os.path.exists(path):
		os.makedirs(path)

def build_clean():
	global project_path, target_path
	src_server_bin_path = os.path.join(project_path, 'src', 'CoreCI.Server', 'bin')
	src_server_obj_path = os.path.join(project_path, 'src', 'CoreCI.Server', 'obj')
	src_worker_bin_path = os.path.join(project_path, 'src', 'CoreCI.Worker', 'bin')
	src_worker_obj_path = os.path.join(project_path, 'src', 'CoreCI.Worker', 'obj')
	web_target_path = os.path.join(project_path, 'web', 'target')

	remove_folder(target_path)
	remove_folder(src_server_bin_path)
	remove_folder(src_server_obj_path)
	remove_folder(src_worker_bin_path)
	remove_folder(src_worker_obj_path)
	remove_folder(web_target_path)

def build_sources(config):
	global project_path
	src_path = os.path.join(project_path, 'src')

	exit_code = subprocess.call(['xbuild', '/p:Configuration=%s' % config, '/verbosity:quiet', 'CoreCI.sln'], cwd = src_path)

def build_webapp(config):
	global project_path
	web_path = os.path.join(project_path, 'web')

	exit_code = subprocess.call(['grunt', '%s-build' % config], cwd = web_path)

def package(src_config, web_config):
	distutils.dir_util.copy_tree(
		os.path.join(project_path, 'src', 'CoreCI.Server', 'bin', src_config),
		os.path.join(target_path, 'server')
	)
	distutils.dir_util.copy_tree(
		os.path.join(project_path, 'src', 'CoreCI.Worker', 'bin', src_config),
		os.path.join(target_path, 'worker')
	)
	distutils.dir_util.copy_tree(
		os.path.join(project_path, 'web', 'target', web_config),
		os.path.join(target_path, 'web')
	)

build_clean()
ensure_folder_exists(target_path)
build_sources(src_config)
build_webapp(web_config)
package(src_config, web_config)
