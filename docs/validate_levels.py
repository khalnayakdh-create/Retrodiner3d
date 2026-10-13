import json, glob, os, random, sys, collections
# Mirrors Assets/Scripts/RetroDiner/Rules/LevelValidator.cs (greedy + random tapper over a visible window).
class Tray:
    def __init__(s, cap): s.cap=cap; s.slots=[]
    def count(s, i): return s.slots.count(i)
    def insert(s, i):
        at=len(s.slots)
        for k in range(len(s.slots)-1,-1,-1):
            if s.slots[k]==i: at=k+1; break
        s.slots.insert(at,i)
        if s.count(i)>=3:
            n=0
            for k in range(len(s.slots)-1,-1,-1):
                if s.slots[k]==i and n<3: del s.slots[k]; n+=1
            return True
        return False
def greedy(spawn, window, cap):
    belt=[]; q=list(spawn); tray=Tray(cap); peak=0
    while len(belt)<window and q: belt.append(q.pop(0))
    guard=len(spawn)*2+10
    while (belt or q) and guard>0:
        guard-=1
        best=None;bs=-10**9
        for i in belt:
            it=tray.count(i); vis=belt.count(i)
            sc=1000 if it==2 else (100+it*10+vis if it+vis>=3 else it*10+vis-50)
            if sc>bs: bs=sc;best=i
        belt.remove(best); trip=tray.insert(best); peak=max(peak,len(tray.slots))
        if not trip and len(tray.slots)>=cap: return False,peak
        if q: belt.append(q.pop(0))
    return (not belt and not q and not tray.slots),peak
def rnd(spawn, window, cap, seed):
    r=random.Random(seed); belt=[]; q=list(spawn); tray=Tray(cap)
    while len(belt)<window and q: belt.append(q.pop(0))
    guard=len(spawn)*2+10
    while belt and guard>0:
        guard-=1
        p=belt.pop(r.randrange(len(belt))); trip=tray.insert(p)
        if not trip and len(tray.slots)>=cap: return False
        if q: belt.append(q.pop(0))
    return not belt and not q and not tray.slots
root=sys.argv[1]
bad=0
for f in sorted(glob.glob(os.path.join(root,'level_*.json'))):
    d=json.load(open(f)); sp=d['spawnList']; w=d['maxItemsOnBelt']; L=d['levelIndex']
    ok,peak=greedy(sp,w,7); wins=sum(rnd(sp,w,7,L*31+i) for i in range(30))/30
    c=collections.Counter(sp); triples=all(v%3==0 for v in c.values())
    acc = ok and triples and (L<=6 or wins<(0.65 if L<=10 else 0.5))
    if not acc: bad+=1
    print(f"L{L:02d} {'OK ' if acc else 'REJ'} items={len(sp):3d} types={len(c)} belt={w:2d} speed={d['beltSpeed']:.2f} greedy={'win' if ok else 'FAIL'} peak={peak} random={wins*100:.0f}% tickets={[(t['itemId'],t['count']) for t in d['tickets']]}")
print("rejected:",bad)
