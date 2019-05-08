# hackiibo
Hackiibo is a portage to .Net of the android [TagMo application](https://github.com/HiddenRamblings/TagMo).

## Limitations:

* Only NTAG215 tags can be used to clone Amiibos. no other type are supported.
* You will require the key files used in the Amiibo encryption.
* Once you write an NFC tag, it is effectively permanent.

## Requirements:
* Amiibo Key Files (Don't ask me for these)
* Some blank NTAG 215 tags
* Amiibo dumps (Don't ask me for these)

## Instructions:

### Writing Amiibo

* Clone this repository
* Make sure NFC reader/writer is plugged in
* Download Amiibo Key Files (in `src/Hackiibo/keys`) & Amiibo dump (in `src/Hackiibo/amiibos`)
* Open the solution and run *Hackiibo* project