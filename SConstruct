import os
import json

current_dir = '.'

source_dir					= current_dir
docs_dir					= os.path.join(source_dir, 'Documentation~')
runtime_source_dir			= os.path.join(source_dir, 'Runtime')
editor_source_dir			= os.path.join(source_dir, 'Editor')
source_file_ext				= "cs"

package_settings_filepath	= os.path.join(source_dir, 'package.json')
with open(package_settings_filepath, 'r') as package_settings_file:
	
	package_settings = json.load(package_settings_file)

	package_name = package_settings["displayName"]
	package_version = package_settings["version"]

docs_root_filepath		= os.path.join(docs_dir, 'Documentation')
readme_filepath			= os.path.join(source_dir, 'README.md')

runtime_source_filepaths	= get_all_subfiles(runtime_source_dir,	extension=source_file_ext)
editor_source_filepaths		= get_all_subfiles(editor_source_dir,	extension=source_file_ext)

source_filepaths = runtime_source_filepaths + editor_source_filepaths

docs = env.Doxygen(
	docs_root_filepath,
	source_filepaths,
	DOXYGEN_PROJECT_NAME=package_name,
	DOXYGEN_USE_MDFILE_AS_MAINPAGE=readme_filepath
)

Clean(docs, docs_dir)