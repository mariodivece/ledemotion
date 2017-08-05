# ledemotion
A very cool LED strip controller for the Raspoberry PI

Here are the steps to get this running

1. Update and upgrade the distro, install mono

```bash
sudo apt-get update
sudo apt-get upgrade
sudo apt-get install mono-complete
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
sudo apt-get update
sudo apt-get dist-upgrade
apt-get autoremove
apt-get clean 
```

Verify the version of mono

```bash
mono version
```

You should get something above 4.6.2

2. Enable SPI
sudo raspi-config

3. Deploy and test continuously
Look in the Support folder. There is a deploy file that uses SshDeploy (get if from github)
To kill the current mono process:
```sudo pkill mono```
To start the mono process again:
```
sudo mono /home/pi/piemotion/Unosquare.LedEmotion.Controller.exe
```
To see a list of processes
```top```
```ps```
```htop``` (might need ```sudo apt-get install htop```)

4. In the command line edit /etc/rc.local
sudo nano /etc/rc.local

5. Then add the following line before exit 0:
mono /home/pi/piemotion/Unosquare.LedEmotion.Controller.exe &

```bash
#!/bin/sh -e
#
# rc.local
#
# This script is executed at the end of each multiuser runlevel.
# Make sure that the script will "exit 0" on success or any other
# value on error.
#
# In order to enable or disable this script just change the execution
# bits.
#
# By default this script does nothing.

# Print the IP address
_IP=$(hostname -I) || true
if [ "$_IP" ]; then
  printf "My IP address is %s\n" "$_IP"
fi

mono /home/pi/piemotion/Unosquare.LedEmotion.Controller.exe &

exit 0
```
