#!/usr/bin/env python

import sys
import xml.etree.ElementTree as ET
from io import StringIO
from typing import Optional


def fqn_name(name):
	return "{http://www.topografix.com/GPX/1/1}" + name


def text_of_child(parent, name) -> Optional[str]:
	element = parent.find(name)
	if element is None or not element.text:
		return None
	return element.text.strip()


def gpx2tsv(file, out=None):
	if out is None:
		out = StringIO("")

	root = ET.parse(file).getroot()

	trk = root.find(fqn_name("trk"))
	assert trk is not None

	print("\t".join(("date", "time", "lat", "lon", "hdop")), file=out)

	for trkseg in trk.iterfind(fqn_name("trkseg")):
		for trkpt in trkseg.iterfind(fqn_name("trkpt")):
			time, date = "None", "None"
			time_date = text_of_child(trkpt, fqn_name("time"))
			if time_date is not None:
				time, date = time_date.strip("Z").split("T")
			# FIXME correct time format

			hdop = str(text_of_child(trkpt, fqn_name("hdop")))

			print("\t".join((date, time, trkpt.get("lat"), trkpt.get("lon"), hdop)), file=out)


def main():
	if len(sys.argv) < 2 or sys.argv[1] == "-":
		if sys.stdin.isatty():
			print("gpx2tsv expects either a filename as first argument or GPX data on stdin", file=sys.stdout)
			sys.exit(1)
		filename = sys.stdin
	else:
		filename = sys.argv[1]
	gpx2tsv(filename, out=sys.stdout)

if __name__ == "__main__":
	main()
