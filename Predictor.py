import pandas as pd
import datetime as dt


def getHourProcListOfDays(df, start, end):
    h = dt.datetime.today().hour
    return df[48 * (start - 1) + h::48]


def updatevarsforui(procs):
    apps = pd.read_csv('apps.csv')
    apps.set_index('num')
    apps.sort_index(inplace=True)
    paths = pd.read_csv('odata.csv')
    paths.drop(['month', 'dom', 'dt', 'hr', 'min', 'tm'], axis=1, inplace=True)
    paths.set_index('exe', inplace=True)
    with open('C:\\Users\\Rick\\Documents\\Rainmeter\\Skins\\ProcessMonitor\\@Resources\\settings.txt',
              'w') as vf:
        #vf.write('[Variables]\n')

        for i in range(0, 5):
            if i < len(procs):
                name = apps.iloc[procs[i]]['proc']
                path = paths.loc[name]['path']
                # print(name, " ", path)
                vf.write(str(name).upper().split('.')[0] + '\n')
                vf.write('"'+path.iloc[0] + '"\n')
            else:
                vf.write('-\n')
                vf.write('""\n')


def predict():
    df = pd.read_csv('simdata.csv')
    df.columns = ['ind', 'procnum']
    df.set_index('ind', inplace=True)
    # print(len(df.index), '\n')
    end = int(len(df.index) / 48)
    if end < 7:
        start = 0
    else:
        start = end - 7
    df = getHourProcListOfDays(df, start, end)
    # print(len(df.index), '\n', start, '\n', end, '\n', df.head(5), '\n', df.tail(5))
    plist = list()
    while len(df.index) > 0:
        p = df.mode().iloc[0]['procnum']
        plist.append(p)
        df = df[df.procnum != p]
        #print('Recommendations (for ',dt.datetime.today().hour,'): ',plist)
    updatevarsforui(plist)
