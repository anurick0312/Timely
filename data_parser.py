import datetime
import pandas as pd
import numpy as np
import Predictor


def parse_data():
    apps = {}
    lasttime = 0
    firsttime = 0
    start = True
    excl = []

    with open('exclusions.txt') as ex:
        line = ex.readline().strip()
        while line:
            excl.append(line.strip())
            line = ex.readline().strip()

    with open('odata.csv', 'r') as f:
        line = f.readline().strip()
        with open('m1.csv', 'w') as op:
            line = f.readline().strip()
            while line:
                apname = line.split(',')[1]
                if excl.__contains__(apname):
                    line = f.readline().strip()
                    continue
                ts = int(line.split(',')[7].split('.')[0])
                if not apps.__contains__(apname):
                    apps[apname] = len(apps)
                if firsttime == 0:
                    firsttime = ts
                if not lasttime == 0:
                    if ts - lasttime >= 1800:
                        lasttime = ts
                        if start:
                            op.write('time,proc\n')
                            start = False
                        op.write(str(datetime.datetime.fromtimestamp(ts)) + ',' + str(apps[apname]) + '\n')
                else:
                    lasttime = ts
                line = f.readline().strip()

    with open('apps.csv', 'w') as af:
        af.write('proc,num\n')
        for a in apps:
            af.write(a + ',' + str(apps[a]) + '\n')

    firstdtav = str(datetime.datetime.fromtimestamp(firsttime)).split(' ')[0]
    firstdt = firstdtav + ' 00:00:00'
    lastdtav = str(datetime.datetime.fromtimestamp(lasttime)).split(' ')[0]
    lastdt = lastdtav + ' 23:59:59'
    bldf = pd.Series(pd.date_range(firstdt, lastdt, freq='30min'))

    df = pd.read_csv('m1.csv')
    df['time'] = pd.to_datetime(df['time'])
    bldf = bldf.to_frame()
    bldf.columns = ['time']
    fdf = pd.concat([df, bldf], ignore_index=True, sort=True)
    fdf.sort_values(by='time', inplace=True)
    fdf = fdf.set_index('time')
    fdf = fdf.fillna(method='ffill')
    fdf = fdf.fillna(0)
    fdf.proc = pd.to_numeric(fdf.proc, downcast='integer')

    fdf.to_csv('m2.csv')

    df = pd.read_csv('m2.csv')
    patternDel = "[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[12456789][1-9]:[0-9]{2},[0-9]+"
    delfilter = df.time.str.contains(patternDel)
    df = df[~delfilter]
    patternDel = "[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[12456789][1-9]:[0-9]{2},[0-9]+"
    delfilter = df.time.str.contains(patternDel)
    df = df[~delfilter]
    timedf = df.set_index('time')

    timedf.to_csv('m2.csv')

    simdf = df.drop(['time'], axis=1)
    simdf.to_csv('simdata.csv')

    Predictor.predict()
