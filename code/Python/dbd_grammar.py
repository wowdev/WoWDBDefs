#! /usr/bin/env python

import dbd
import modgrammar
import sys

sys.stdout.writelines(modgrammar.generate_ebnf(dbd.dbd_file, wrap=None))