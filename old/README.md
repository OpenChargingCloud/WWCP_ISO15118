# WWCP ISO/IEC 15118

This software allows communication between World Wide Charging Protocol (WWCP) entities and
entities implementing ISO 15118 — _Signal Level Attenuation Characterization (SLAC)_, _SECC
Discovery Protocol (SDP)_, _Vehicle-To-Grid Transport Protocol (V2GTP)_, _ISO/IEC 15118-2_,
_ISO/IEC 15118-20_ and _ISO/IEC 15118-8_ for wireless communication.

The focus is the communication between an electric vehicle and an e-mobility charging station.


## Differences to the official protocol specification

The following describes differences of this implementation to the official protocol specification.
Most changes are intended to simplify the daily operations business, high availability or to support
additional concepts/methods like *European General Data Protection Regulation (GDPR)* and the
*German Calibration Law (Eichrecht)*.

- Class and data type names do not strictly reflex the names defined within the XML schemata.
  - E.g. all collection property name have plural names like "authentication element**s**" instead of "authentication element".
  - Request and response classes do not use the short "Req" or "Res" words.
  - The name of ENUM classes always have plural names.
  - Helper classes for collections are avoided and _IEnumerable<...>_ is used instead.
- ...

That applies to the hand-written WWCP-facing types. The **generated** EXI codec is the opposite by
design: its type names follow the XSD exactly, because they are what a byte diff against cbV2G or
EXIficient gets read in.


### Your participation

This software is free and Open Source under [GNU Affero General Public License (AGPL)](LICENSE).
We appreciate your participation in this ongoing project, and your help to
improve it and the e-mobility ICT in general. If you find bugs, want to request
a feature or send us a pull request, feel free to use the normal GitHub
features to do so. For this please read the Contributor License Agreement
carefully and send us a signed copy or use a similar free and open license.
