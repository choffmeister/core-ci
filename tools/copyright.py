#!/usr/bin/python

import os
import re

exclude_dirs = ['.\\lib']
utf_header = chr(0xef)+chr(0xbb)+chr(0xbf)
copyright = file('COPYRIGHT','r+').read()
comment_regex = re.compile('^\s*(//|\*|/*|\*/)')

def is_comment_line(line):
	return line.lstrip().startswith('//') or line.lstrip().startswith('*') or line.lstrip().startswith('/*') or line.lstrip().startswith('*/')

def strip_file_comment(lines):
	global comment_regex

	while len(lines) > 0:
		if is_comment_line(lines[0]) or lines[0] is '':
			lines = lines[1:]
		else:
			break
	return lines

def strip_empty_lines(lines):
	while len(lines) > 0:
		if lines[0] is '':
			lines = lines[1:]
		else:
			break
	while len(lines) > 0:
		if lines[-1] is '':
			lines = lines[:-1]
		else:
			break
	return lines

def update_source(filename):
	global exclude_dirs
	global utf_header
	global copyright

	# open file for reading
	file_in = file(filename, 'r+')

	# read content
	content = file_in.read()

	# remove UTF-8 header
	if (content.startswith(utf_header)): content = content[3:]

	# split into lines, strip right
	lines = content.split('\n')
	lines = map(lambda s: s.rstrip(), lines)

	# remove old file comment and strip outer empty lines
	lines = strip_file_comment(lines)
	lines = strip_empty_lines(lines)

	# open file again for writing
	file_out = file(filename, 'w')

	# write UTF-8 header
	file_out.write(utf_header)

	# write copyright header
	file_out.write(copyright)

	for line in lines:
		file_out.write(line.rstrip() + '\n')

# traverse of all C# files
def recursive_traversal(dir):
	global exclude_dirs

	for name in os.listdir(dir):
		path = os.path.join(dir, name)

		if (os.path.isdir(path) and not path in exclude_dirs):
			recursive_traversal(path)
		elif (os.path.isfile(path) and path.endswith('.cs')):
			update_source(path)

script_path = os.path.realpath(__file__)
project_path = os.path.join(os.path.dirname(script_path), '..')
recursive_traversal(project_path)
