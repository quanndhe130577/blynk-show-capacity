#!/usr/bin/python3

import time
import datetime
import RPi.GPIO as GPIO
import sqlite3
import os
import requests
import json
import socket

UDP_IP = "127.0.0.1"
UDP_PORT = 8000

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # UDP

# set up pin used on Raspberry pi
pin = 18
bounceTime = 5
timeoutBlink = 0.012
GPIO.setmode(GPIO.BCM)
GPIO.setup(pin, GPIO.IN, pull_up_down=GPIO.PUD_DOWN)

previousTime = None
previous = None
blinkTime = None
logging = False

def Log(message):
        if logging:
         print(message)
         try:
          with open("error.log", "a") as myfile:
            myfile.write(message + "\r\n")
         except:
          pass



def onChange(channel):
    global previousTime
    global previous
    global blinkTime
    dateNow = datetime.datetime.now()
    now = time.time()
    current = GPIO.input(pin)
    if previous == True  and current == False:
      if blinkTime != None:
        blinkDelta = now - blinkTime
        blinkTime = None
        if blinkDelta >= timeoutBlink:
          previous = None
          previousTime = None
          Log("Now " + str(dateNow) + " Blinktime error " + str(blinkDelta) + " [s]")
          return
      if previousTime != None:
        delta = now - previousTime
        power = 3600.0 / delta
        ipower = int(round(power))

        all = "Now " + str(dateNow) + " Falling delta=" + str(delta) + " [s] power=" + str(ipower) + " [W]"
        Log (all)
        try:
         message = str(pin) + " " + str(round(power,1))
         bytes = message.encode()
         sock.sendto(bytes, (UDP_IP, UDP_PORT))
        except:
         pass
      else:
         Log("Now "+ str(dateNow) + " Not a valid previous time yet")
      blinkTime = None
      previousTime = now
      previous = None
    elif current == True:
      if previous == None:
        previous = current
        blinkTime = now
        Log("Now " + str(dateNow) + " Rising")
      else:
        previous = None
        blinkTime = None
        previousTime = None
        Log("Now " + str(dateNow) + " Error. Double rising. Resetting...")
    else:
      previous = None
      blinkTime = None
      previousTime = None
      Log("Now " + str(dateNow) + " Resetting...")



GPIO.add_event_detect(pin, GPIO.BOTH, callback=onChange, bouncetime=bounceTime)
Log("Start monitor")
while True:
    time.sleep(1000)

GPIO.cleanup()
