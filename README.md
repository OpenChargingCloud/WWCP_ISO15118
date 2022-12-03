# WWCP ISO/IEC 15118-20

This software will allow the communication between World Wide Charging
Protocol (WWCP) entities and entities implementing
_ISO/IEC 15118-20_ and _ISO/IEC 15118-8_ for wireless communication.
The focus of this protocol are the communication aspects between an
electric vehicle and an e-mobility charging station.


## Implementation details

In order to make development and debugging 1024 times easier, this _ISO/IEC 15118_ implementation comes with additional JSON (de-)serialization. As, as far as we know, no other project has yet defined a JSON schemata for ISO/IEC 15118, we defined our own, which adopts some JSON-LD concepts like the "@context" property.


## Differences to the official protocol specification

The following desribes differences of this implementation to the official protocol specification.
Most changes are intended to simplify the daily operations business, high availability or to support additional concepts/methods like *European General Data Protection Regulation (GDPR)*  and the *German Calibration Law (Eichrecht)*.

- Class and data type names do not strictly reflex the names defined within the XML schemata.
  - E.g. all collection property name have plural names like "authentication element**s**" instead of "authentication element".
  - Request and response classes do not use the short "Req" or "Res" words.
  - The name of ENUM classes always have plural names.
  - Helper classes for collections are avoided and _IEnumerable<...>_ is used instead.
- ...


### Your participation

This software is Open Source under the Apache 2.0 license. We appreciate
your participation in this ongoing project, and your help to improve it
and the e-mobility ICT in general. If you find bugs, want to request a
feature or send us a pull request, feel free to use the normal GitHub
features to do so. For this please read the Contributor License Agreement
carefully and send us a signed copy or use a similar free and open license.
