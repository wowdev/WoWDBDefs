#!/bin/bash

./dump_relations.py \
  --definitions ../../definitions/  \
  $(ls ../../definitions/*Visual* ../../definitions/*Model* \
      | sed -e 's,../../definitions/,--only ,' -e 's,.dbd$,,') \
  | dot -Tpdf > t.pdf
