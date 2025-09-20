/* FNA3D - 3D Graphics Library for FNA
 *
 * Copyright (c) 2020-2021 Ethan Lee
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */

#ifndef FNA3D_DRIVER_D3D11_H
#define FNA3D_DRIVER_D3D11_H

#define D3D11_NO_HELPERS
#define CINTERFACE
#define COBJMACROS
#include <d3d11.h>

/* IID Imports from https://magnumdb.com */

static const IID D3D_IID_IDXGIFactory1 = {0x770aae78,0xf26f,0x4dba,{0xa8,0x29,0x25,0x3c,0x83,0xd1,0xb3,0x87}};
static const IID D3D_IID_IDXGIFactory2 = {0x50c83a1c,0xe072,0x4c48,{0x87,0xb0,0x36,0x30,0xfa,0x36,0xa6,0xd0}};
static const IID D3D_IID_ID3D11Texture2D = {0x6f15aaf2,0xd208,0x4e89,{0x9a,0xb4,0x48,0x95,0x35,0xd3,0x4f,0x9c}};
static const IID D3D_IID_ID3DUserDefinedAnnotation = {0xb2daad8b,0x03d4,0x4dbf,{0x95,0xeb,0x32,0xab,0x4b,0x63,0xd0,0xab}};

/* VS2010 / DirectX SDK Fallback Defines */

#ifndef DXGI_FORMAT_B4G4R4A4_UNORM
#define DXGI_FORMAT_B4G4R4A4_UNORM (DXGI_FORMAT) 115
#endif

#ifndef D3D_FEATURE_LEVEL_11_1
#define D3D_FEATURE_LEVEL_11_1 (D3D_FEATURE_LEVEL) 0xb100
#endif

/* D3D Function Typedefs */

typedef HRESULT(WINAPI *PFN_D3DCOMPILE)(
    LPCVOID pSrcData,
    SIZE_T SrcDataSize,
    LPCSTR pSourceName,
    const D3D_SHADER_MACRO *pDefines,
    ID3DInclude *pInclude,
    LPCSTR pEntrypoint,
    LPCSTR pTarget,
    UINT Flags1,
    UINT Flags2,
    ID3DBlob **ppCode,
    ID3DBlob **ppErrorMsgs
);

/* ID3DUserDefinedAnnotation */
/* From d3d11_1.h, cleaned up a bit... */

typedef struct ID3DUserDefinedAnnotation ID3DUserDefinedAnnotation;
typedef struct ID3DUserDefinedAnnotationVtbl
{
	HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
		ID3DUserDefinedAnnotation * This,
		REFIID riid,
		void **ppvObject);

	ULONG ( STDMETHODCALLTYPE *AddRef )(
		ID3DUserDefinedAnnotation * This);

	ULONG ( STDMETHODCALLTYPE *Release )(
		ID3DUserDefinedAnnotation * This);

	INT ( STDMETHODCALLTYPE *BeginEvent )(
		ID3DUserDefinedAnnotation * This,
		LPCWSTR Name);

        INT ( STDMETHODCALLTYPE *EndEvent )(
            ID3DUserDefinedAnnotation * This);

	void ( STDMETHODCALLTYPE *SetMarker )(
		ID3DUserDefinedAnnotation * This,
		LPCWSTR Name);

	BOOL ( STDMETHODCALLTYPE *GetStatus )(
		ID3DUserDefinedAnnotation * This);
} ID3DUserDefinedAnnotationVtbl;

struct ID3DUserDefinedAnnotation
{
	struct ID3DUserDefinedAnnotationVtbl *lpVtbl;
};

#define ID3DUserDefinedAnnotation_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ID3DUserDefinedAnnotation_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ID3DUserDefinedAnnotation_Release(This)	\
	( (This)->lpVtbl -> Release(This) )

#define ID3DUserDefinedAnnotation_BeginEvent(This,Name)	\
	( (This)->lpVtbl -> BeginEvent(This,Name) )

#define ID3DUserDefinedAnnotation_EndEvent(This)	\
	( (This)->lpVtbl -> EndEvent(This) )

#define ID3DUserDefinedAnnotation_SetMarker(This,Name)	\
	( (This)->lpVtbl -> SetMarker(This,Name) )

#define ID3DUserDefinedAnnotation_GetStatus(This)	\
	( (This)->lpVtbl -> GetStatus(This) )

#endif /* FNA3D_DRIVER_D3D11_H */
