
XMLFILES:=$(wildcard *.xml)
STUBFILES:=$(patsubst %.xml,%.stub.cs,$(XMLFILES))
IDLFILES:=$(patsubst %.xml,%.idl.cs,$(XMLFILES))
APIDLLS:=$(patsubst %.stub.cs,bin/api/api.%.dll,$(STUBFILES))
IDLDLLS:=$(patsubst %.idl.cs,bin/idl/idl.%.dll,$(IDLFILES))

IDLDLL:=../idl/bin/Debug/idl.dll
apimono:=apimono/bin/Debug/apimono.exe

all:	$(STUBFILES) $(IDLFILES) $(APIDLLS) $(IDLDLLS)

%.idl.cs:	%.xml $(apimono)
	apimono/bin/Debug/apimono.exe $< -idl:$@

%.stub.cs:	%.xml $(apimono)
	rm -f $@.new
	$(apimono) $< -stub:$@.new
	[ -s $@.base ] || cp $@.new $@.base
	[ -s $@ ] || cp $@.new $@
	-@diff3 -a -A -m $@ $@.base $@.new > $@.merge || echo "WARNING: conflicts in updated stubs"
	mv $@ $@~
	mv $@.merge $@
	cp $@.new $@.base

bin/api/api.%.dll:	%.stub.cs $(IDLDLL)
	gmcs -target:library -reference:$(abspath $(IDLDLL)) $< -out:$@ -debug

bin/idl/idl.%.dll:	%.idl.cs $(IDLDLL)
	gmcs -target:library -reference:$(abspath $(IDLDLL)) $< -out:$@ -debug

clean:
	rm -f bin/api/* bin/idl/* *.idl.cs *.new *.merge *~

spotless:	clean
	rm *.base
