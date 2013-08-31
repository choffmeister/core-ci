#!/usr/bin/python

import os
import re

exclude_dirs = ['.\\lib', '.\\CoreCI.WebApp\\bower_components', '.\\CoreCI.WebApp\\node_modules']
utf_header = chr(0xef)+chr(0xbb)+chr(0xbf)
copyright = file('COPYRIGHT','r+').read()

def strip_file_comment(lines, comment_regex):
	compiled_regex = re.compile(comment_regex)

	while len(lines) > 0:
		if compiled_regex.match(lines[0]) is not None or lines[0] is '':
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

def update_source(filename, start, pre, end, comment_regex):
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
	lines = strip_file_comment(lines, comment_regex)
	lines = strip_empty_lines(lines)

	# open file again for writing
	file_out = file(filename, 'w')

	# write UTF-8 header (deactivated, since grunt does not support UTF-8 header in Gruntfile)
	# file_out.write(utf_header)

	# write copyright header
	copyright_lines = copyright.split('\n')
	copyright_lines = strip_empty_lines(copyright_lines)
	file_out.write(start + '\n')
	for copyright_line in copyright_lines:
		file_out.write((pre + copyright_line).rstrip() + '\n')
	file_out.write(end + '\n')

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
			update_source(path, '/*', ' * ', ' */', '^(\s*//|\s*\*|\s*/\*|\s*\*/)')
		elif (os.path.isfile(path) and path.endswith('.coffee')):
			update_source(path, '###', '  ', '###', '^(###|\s+)')
		elif (os.path.isfile(path) and path.endswith('.jade')):
			update_source(path, '//-', '//- ', '//-', '^(//\-|\s+)')
		elif (os.path.isfile(path) and path.endswith('.less')):
			update_source(path, '/*', ' * ', ' */', '^(\s*//|\s*\*|\s*/\*|\s*\*/)')
script_path = os.path.realpath(__file__)
project_path = os.path.join(os.path.dirname(script_path), '..')
recursive_traversal(project_path)
