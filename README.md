# ImageImporter
A simple utility for importing photos and avoiding duplicates. Note:
This has only been tested with a memory card from a Nikon D7000. It
shouldn't ever delete any files, but use at your own peril.

I was sick of using applications that took forever to import images because they wanted to show you previews of each image. I also had issues with some applications not excluding duplicates like they were configured to do. I opted to spend a few hours and some stackoverflow searches to create my own! ImageImporter creates a hidden file with SHA1 hashes of everything it has seen before (directories and files) and compares the source location to this list before copying any files. 

The program is optimized to work with how Nikon stores images on memory cards so I don't have to revisit every file again. This cuts scanning down to about 10 seconds or less for 5000 files (about 200 files in 28 folders).

On successful import, a new folder with today's date will be created in the destination directory. If the destination is already created, the files will be added to the existing directory. Additionally, if any files exist with the same name but have different SHA1 hashes, the file will be renamed upon copy so as not to destroy any files in the destination directory.

Usage:

ImageImporter.exe --source H:\ --destination D:\Photos
  - This will create a folder in D:\Photos with today's date if any files in H:\ are not present in D:\Photos
  
For ease of use, I created a desktop shortcut with the command-line arguments setup. This way it is a matter of popping in the memory card and doubling clicking the shortcut.
