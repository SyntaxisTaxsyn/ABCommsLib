# ABCommsLib

This project is a wrapper of the excellent LibPLCTag library specifically geared at Allen Bradley Control Logix PLCs with the view to making
the declaration and addition of tags to a PLC controller program object even easier. 

Includes an active tag feature that when turned on periodically polls the configured tag and exposes a DataChanged event which can be used to 
drive other program logic based on the state change of the tag.

A PLC ping utility has been included to check the configured PLC exists before attmepting to instantiate the tags (only checks PLC IP is active)

Unit tests are included for confimration of functionality N.B Unit tests require a PLC with the tags configured to work :)
