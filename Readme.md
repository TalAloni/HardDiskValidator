About Hard Disk Validator:
==========================
This simple utility was designed to help you find out if your hard drive has reached its end of life.  

##### Q: What is a bad sector?  
Hard drives write data in block units (sectors), every time a hard drive update a sector, it also updates a checksum (stored immediately after the sector data). When a sector is read from your hard drive, it's expected that the sector checksum will match the sector data, if that is not a case, the hard disk knows something went wrong during the write operation, that's called a bad sector.  

##### Q: What causes bad sectors to happen?  
Power failure during write is one common reason, hard drive malfunction is another common reason.  

##### Q: Can I repair a bad sector? 
Well, the data stored on the sector is invalid, but if your hard drive is functioning properly, you can overwrite the bad sector (and now it won't be "bad" anymore since the sector checksum will be updated as well).  

###### This is my hard drive after wiping out a bad sector that was created during a power failure:  
![HardDiskValidator](http://vm1.duckdns.org/Public/HardDiskValidator/HardDiskValidator.png)

##### Q: Which test should I use? 
If you're recovering from a power failure, then "Read + Wipe Damaged + Read" is the fastest way to wipe out the bad sectors and avoid any issue with software that does not respond well to bad sectors.  
If something seems to be wrong with the drive, it's better to back up the data and use the "Write + Verify" test, which will erase all of the data on the disk. 

##### Q: What's the difference between the tests? 
* **Read:** Will scan the entire hard drive surface to find bad sectors.  
* **Read + Wipe Damaged + Read:** Will scan the entire hard drive surface to find bad sectors, if bad sectors are found, they will be overwritten, and read again to make sure they were written successfully this time. 
* **Read + Write + Verify + Restore:** The program will write a test pattern to the disk, verify the pattern was written successfully, and then restore the original data. 
* **Write + Verify:** The program will write a test pattern to the disk and verify the pattern was written successfully. (the original data will be lost). 

##### Q: I only have a single hard disk that is used to boot my OS, how can I test it? 
You should have no problem performing a read test, for any other test it's recommended that you avoid testing the disk that hosts the currently running operating system. so you have two options:   
&nbsp;&nbsp; **1.** Connect your disk (as a secondary disk) to another PC to test it there.   
&nbsp;&nbsp; **2.** Boot your PC from Windows PE (from a CD, DVD or USB) and use Mono to launch Hard Disk Validator. I have packaged the necessary Mono files [here](http://vm1.duckdns.org/public/HardDiskValidator/Mono-3.2.3.zip). You can find a ready-to-use WinPE 5.1 ISO [here](https://drive.google.com/open?id=0B1wrdynUizpMOWEwWFBnMlBkRUU). 
![HardDiskValidator on WinPE](http://vm1.duckdns.org/Public/HardDiskValidator/HardDiskValidator-WinPE.png)

Contact:
========
If you have any question, feel free to contact me.  
Tal Aloni <tal.aloni.il@gmail.com>
