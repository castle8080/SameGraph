import sys
import re

STAT_RE = re.compile(r".*Time=(\d+).*Nodes=(\d+).*Edges=(\d+).*")

print(",".join(["Time", "Nodes", "Edges"]))
for line in sys.stdin:
    line = line.strip()
    md = STAT_RE.match(line)
    if md:
        time, nodes, edges = md.group(1), md.group(2), md.group(3)
        print(",".join([time, nodes, edges]))


