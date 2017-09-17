//
// Created by MAS on 31/01/2016.
//

#include <string.h>
#include <stdlib.h>

#include "util.h"
#include "nfc3d/amiibo.h"
#include "nfc3d/keygen.h"

static nfc3d_amiibo_keys keys;

typedef unsigned char byte;

int setKeysUnfixed(byte* keydata, int dataLength) {
	if (sizeof(keys.data) != dataLength)
		return 0;
	
	memcpy(&(keys.data), keydata, dataLength);

	return 1;
}

int setKeysFixed(byte* keydata, int dataLength) {
	if (sizeof(keys.tag) != dataLength)
		return 0;
	
	memcpy(&(keys.tag), keydata, dataLength);

	return 1;
}

int unpack(byte* tag, int dataLength, byte* returnData, int returnDataLength) {
	
	if (dataLength< NFC3D_AMIIBO_SIZE || returnDataLength< NFC3D_AMIIBO_SIZE || dataLength != returnDataLength)
		return 0;
	
	uint8_t* original = (uint8_t*)malloc(dataLength * sizeof(uint8_t));
	uint8_t modified[NFC3D_AMIIBO_SIZE];
	
	memcpy(original, tag, dataLength);
	
	if (!nfc3d_amiibo_unpack(&keys, original, modified))
		return 0;
	
	byte* bufferPtr = returnData;
	memcpy(bufferPtr, original, dataLength); //copy any extra data in source to destination
	memcpy(bufferPtr, modified, NFC3D_AMIIBO_SIZE);

	free(original);

	return 1;
}

int pack(byte* tag,int dataLength, byte* returnData, int returnDataLength) {
	
	if (dataLength< NFC3D_AMIIBO_SIZE || returnDataLength< NFC3D_AMIIBO_SIZE || dataLength != returnDataLength)
		return 0;
	
	uint8_t* original = (uint8_t*)malloc(dataLength * sizeof(uint8_t));
	uint8_t modified[NFC3D_AMIIBO_SIZE];
	
	memcpy(original, tag, dataLength);
	
	nfc3d_amiibo_pack(&keys, original, modified);
	
	byte* bufferPtr = returnData;
	memcpy(bufferPtr, original, dataLength); //copy any extra data in source to destination
	memcpy(bufferPtr, modified, NFC3D_AMIIBO_SIZE);
	
	free(original);

	return 1;
}
