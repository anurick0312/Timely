import datetime
import signal
from time import sleep
from os import path
import shutil

from psutil import AccessDenied

from ScheduledRunner import RepeatedTimer
import psutil

import data_parser

proc_list = []
new_proc_list = []
new_procs = []

exclusions = []

with open('exclusions.txt', 'r') as ex:
    line = ex.readline().strip()
    while line:
        exclusions.append(line)
        line = ex.readline().strip()


# print(exclusions)

def getprocess_file_name(path):
    tokens = path.split('\\')
    return tokens[tokens.__len__() - 1]


def is_necessary(path):
    tokens = path.split('\\')
    if tokens[0] == 'C:' and tokens[1] == 'Windows':
        return False
    if tokens[0] == 'C:' and tokens[1] == 'Users':
        return False
    if exclusions.__contains__(getprocess_file_name(path)):
        return False
    if tokens.__contains__('AppData'):
        return False
    return True


def save(ptosave):
    f = open('data.csv', 'a+')
    # print(ptosave)
    for pt in ptosave:
        ps = pt[0]
        if not is_necessary(ps):
            continue
        psts = pt[1]
        month = datetime.datetime.fromtimestamp(psts).strftime('%m')
        dayofmonth = datetime.datetime.fromtimestamp(psts).strftime('%d')
        dayofweek = datetime.datetime.fromtimestamp(psts).strftime('%w')
        hour = datetime.datetime.fromtimestamp(psts).strftime('%H')
        min = datetime.datetime.fromtimestamp(psts).strftime('%M')
        data = ps + "," + getprocess_file_name(ps) + "," + month + "," + dayofmonth + "," + dayofweek + \
               "," + hour + "," + min + "," + str(psts)
        # print(data)
        f.write(data + "\n")
        print("Logged for : '" + getprocess_file_name(ps) +
              "' at " + datetime.datetime.fromtimestamp(psts).strftime('%d-%m-%y %H:%M'))
    f.close()
    src = path.realpath('data.csv')
    head, tail = path.split(src)
    dst = head + "\odata.csv"
    shutil.copy(src, dst)
    shutil.copystat(src, dst)
    data_parser.parse_data()


def fetch_process_list():
    global proc_list, new_procs
    for proc in psutil.process_iter():
        try:
            new_proc_list.append((proc.exe(), proc.create_time()))
        except AccessDenied:
            continue
    if proc_list.__len__() == 0:
        print("Monitor Running...")
        proc_list = new_proc_list.copy()
    else:
        new_procs = list(set(new_proc_list) - set(proc_list))
        proc_list = new_proc_list.copy()
        save(new_procs)
    new_proc_list.clear()


print("Monitor Starting...")
fetch_process_list()
rt = RepeatedTimer(30, fetch_process_list)
# pt = RepeatedTimer(35, data_parser.parse_data)  # it auto-starts, no need of rt.start()
# try:
#     sleep(100)  # your long-running job goes here...
# finally:
#     rt.stop()
#     f.close()
#     print("File closed\nScript terminated successfully")
#     exit(0)
# better in a try/finally block to make sure the program ends!

# datetime.datetime.fromtimestamp(myNumber).strftime('%Y-%m-%d %H:%M:%S')
