# Listen für Module anfertigen

import sys, mysql.connector
import random
from random import sample
from os import environ
from json import loads

# Funktion Modulliste:

def modulliste(i):
    global cursor
    a="select * from modulwahl where "
    b="=1"               # diese Iteration funktioniert:)
    cursor.execute(a+i+b)
    j=set()
    for k in cursor:
        j.add(k[0])
    return(j)

# Schülerliste für die Schiene anlegen

def schuelerliste():
    global cursor
    cursor.execute("select*from modulwahl")

    sl = set()
    for k in cursor:
        sl.add(k[0])
    return sl

# Schüler auswählen und zuteilen

def zuteilen(source, students): # source = eine Modulliste; students = freie SuS
    # SuS wählen
    picked = []

    while len(picked) < 22:
        current = sample(source, 50 if len(source) >= 50 else len(source))
        counter = 0
        for s in source:    # gibt es noch einen freien SuS?
            if s in students:
                break       # wenn freier SuS, Abbruch
            else:
                counter += 1

        if counter == len(source):
            break       # beendet äußere while-loop

        for s in current:   # Topf wird gefüllt
            if len(picked) >= 22:
                break
            if s not in picked and s in students:
                picked.append(s) # wenn SuS in current & kein Modul dann picked
                students.discard(s)
                source.discard(s)
        if len(source) == 0 or len(students) == 0:
            break        

    # Rückgabe der Kurslisten
    return picked

# Modul füllen

random.seed()

try:
    config=loads(environ['limoinput_connection'])
    connection = mysql.connector.connect(**config)
    print("Verbindung erfolgreich")
    print()
except Exception as e:
    print("Verbindung fehlgeschlagen: {}".format(str(e)))

cursor = connection.cursor()

# Modullisten erstellen

deutsch = 10
english = 10
mathe = 10
franz = 4
segel = 8
langen = [deutsch, english, mathe, franz]
prefix = ['D', 'E', 'M', 'F']

modullisten = []
segels = []

for i, j in zip(langen, prefix):                    # zip gibt Tupel zurück solange möglich    #hier 4 Tupel
    for k in range(i):                              # für Deu 10 Kurse, für E 10 Kurse usw.
        modullisten.append((modulliste('{}{}'.format(j, k + 1)), '{}{}'.format(j, k + 1)))

for i in range(segel):
    segels.append((modulliste('S{}'.format(i + 1)), 'S{}'.format(i + 1)))


sl = set()
result = []

for i in range(9):
    modullisten.sort(reverse=True)
    sl = schuelerliste()
    A = []
    for j in range(8):
        A.append((zuteilen(modullisten[j][0], sl), modullisten[j][1]))

    if i == 8:
        r = random.randint(0, 7)
        A.append((zuteilen(segels[r][0], sl), segels[r][1]))
    else:
        A.append((zuteilen(segels[i][0], sl), segels[i][1]))

    A.sort()
    bias = 0
    while len(sl) > 0:
        for k in range(len(A)):
            number = (22 + bias) - len(A[k][0])
            picked = sample(sl, number if len(sl) >= number else len(sl))
            A[k][0].extend(picked)
            for s in picked:
                if s in sl:
                    sl.discard(s)
        bias += 1

    result.append(A)

 
for A, i in zip(result, range(len(result))):
    print("***************************************************************************\n")
    print("\nBegin Section {}: ".format(i + 1))
    seen = {}
    for modul in A:
        print("{}: {}\n\tAnzahl Schueler: {}\n".format(modul[1], modul[0], len(modul[0])))
        for s in modul[0]:
            if s in seen:
                print('Student {} is in multiple modules!'.format(s))
            else:
                seen[s] = s

    print("***************************************************************************\n")

for i in range(len(result)):
    f = open('./RESULTS/section{}.txt'.format(i + 1), 'a')
    for modul in result[i]:
        f.write("{}: {}\n\tAnzahl Schueler: {}\n".format(modul[1], modul[0], len(modul[0])))
    f.close()
