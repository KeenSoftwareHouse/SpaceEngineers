// MyTSF.h

#pragma once


using namespace System;

namespace MyTSF {

	//文字列変換用クラス
	//IMEを使って文字列をローマ字からかな交じり文に変換する.
	public ref class MyIME
	{
		// TODO: このクラスの、ユーザーのメソッドをここに追加してください。
	public:
		static void IMEStart(Int32 hwnd);
		static void IMEStart(IntPtr hwnd);
		static void IMEEnd();
		static void SetTarget(String^ rTarget);
		static String^ PreConvert();
		static void SetConvert();
		static String^ Convert();
		static void ResetConvert();
		static Int32 ConvertAbleCount();
	};

	class MyTextStore : public ITextStoreACP
	{
	public:
		MyTextStore();
		virtual ~MyTextStore();
		bool IsLock(DWORD iFlags);
		bool SetLock(DWORD iFlags);
		bool ResetLock();
		bool SetString(std::wstring istring);

		#pragma region Regular Use Methods
		ULONG STDMETHODCALLTYPE AddRef();
		HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,void **ppvObject);
		ULONG STDMETHODCALLTYPE Release();

		STDMETHODIMP AdviseSink(REFIID riid, IUnknown* punk, DWORD dwMask);
		STDMETHODIMP UnadviseSink(IUnknown* punk);
		STDMETHODIMP RequestLock(DWORD dwLockFlags, HRESULT* phrSession);
		STDMETHODIMP QueryInsert(LONG acpTestStart, LONG acpTestEnd, ULONG cch, LONG* pacpResultStart, LONG* pacpResultEnd);
		STDMETHODIMP GetText(LONG acpStart, LONG acpEnd, PWSTR pchPlain, ULONG cchPlainReq, ULONG* pcchPlainOut, TS_RUNINFO* prgRunInfo, ULONG ulRunInfoReq, ULONG* pulRunInfoOut, LONG* pacpNext);
		STDMETHODIMP GetStatus(TS_STATUS* pdcs);
		STDMETHODIMP GetSelection(ULONG ulIndex, ULONG ulCount, TS_SELECTION_ACP* pSelection, ULONG* pcFetched);
		#pragma endregion

		#pragma region No Defined Methods
		STDMETHODIMP SetSelection(ULONG ulCount, TS_SELECTION_ACP const* pSelection){return E_NOTIMPL;}
		STDMETHODIMP SetText(DWORD dwFlags, LONG acpStart, LONG acpEnd, PCWSTR pchText, ULONG cch, TS_TEXTCHANGE* pChange){return E_NOTIMPL;}
		STDMETHODIMP GetFormattedText(LONG acpStart, LONG acpEnd, IDataObject** ppDataObject){return E_NOTIMPL;}
		STDMETHODIMP GetEmbedded(LONG acpPos, REFGUID rguidService, REFIID riid, IUnknown** ppunk){return E_NOTIMPL;}
		STDMETHODIMP QueryInsertEmbedded(GUID const* pguidService, FORMATETC const* pFormatEtc, BOOL* pfInsertable){return E_NOTIMPL;}
		STDMETHODIMP InsertEmbedded(DWORD dwFlags, LONG acpStart, LONG acpEnd, IDataObject* pDataObject, TS_TEXTCHANGE* pChange){return E_NOTIMPL;}
		STDMETHODIMP RequestSupportedAttrs(DWORD dwFlags, ULONG cFilterAttrs, TS_ATTRID const* paFilterAttrs){return E_NOTIMPL;}
		STDMETHODIMP RequestAttrsAtPosition(LONG acpPos, ULONG cFilterAttrs, TS_ATTRID const* paFilterAttrs, DWORD dwFlags){return E_NOTIMPL;}
		STDMETHODIMP RequestAttrsTransitioningAtPosition(LONG acpPos, ULONG cFilterAttrs, TS_ATTRID const* paFilterAttrs, DWORD dwFlags){return E_NOTIMPL;}
		STDMETHODIMP FindNextAttrTransition(LONG acpStart, LONG acpHalt, ULONG cFilterAttrs, TS_ATTRID const* paFilterAttrs, DWORD dwFlags, LONG* pacpNext, BOOL* pfFound, LONG* plFoundOffset){return E_NOTIMPL;}
		STDMETHODIMP RetrieveRequestedAttrs(ULONG ulCount, TS_ATTRVAL* paAttrVals, ULONG* pcFetched){return E_NOTIMPL;}
		STDMETHODIMP GetEndACP(LONG* pacp){return E_NOTIMPL;}
		STDMETHODIMP GetActiveView(TsViewCookie* pvcView){return E_NOTIMPL;}
		STDMETHODIMP GetACPFromPoint(TsViewCookie vcView, POINT const* pt, DWORD dwFlags, LONG* pacp){return E_NOTIMPL;}
		STDMETHODIMP GetTextExt(TsViewCookie vcView, LONG acpStart, LONG acpEnd, RECT* prc, BOOL* pfClipped){return E_NOTIMPL;}
		STDMETHODIMP GetScreenExt(TsViewCookie vcView, RECT* prc){return E_NOTIMPL;}
		STDMETHODIMP GetWnd(TsViewCookie vcView, HWND* phwnd){return E_NOTIMPL;}
		STDMETHODIMP InsertTextAtSelection(DWORD dwFlags, PCWSTR pchText, ULONG cch, LONG* pacpStart, LONG* pacpEnd, TS_TEXTCHANGE* pChange){return E_NOTIMPL;}
		STDMETHODIMP InsertEmbeddedAtSelection(DWORD dwFlags, IDataObject* pDataObject, LONG* pacpStart, LONG* pacpEnd, TS_TEXTCHANGE* pChange){return E_NOTIMPL;}
		#pragma endregion

	protected:
	public:
		volatile long now_ref_counter;
		volatile long ref_flgs;
		unsigned long all_ref_count;
		ITextStoreACPSink* rsink;
		DWORD mask;
		std::wstring text;
	};

	class MyIMEBase
	{
	public:
		static void IMEStart(HANDLE hwnd);
		static void IMEEnd();
		static void SetTarget(String^ rTarget);
		static String^ PreConvert();
		static String^ Convert();
		static void SetConvert();
		static void ResetConvert();
		static Int32 ConvertAbleCount();
		template<typename T>
		static void RELEASE(CComPtr<T> x)
		{
			if(x != NULL)
			{
				x = NULL;
			}
		}
		template<typename T>
		static void RELEASE(T* x)
		{
			if(x != NULL)
			{
				x->Release();
				x = NULL;
			}
		}
	protected:
		static std::wstring target;
		static std::wstring htarget;
		static std::queue<std::wstring> outRef;

		static CComPtr<ITfThreadMgr> thr_mgr;
		static CComPtr<ITfDocumentMgr> doc_mgr;
		static CComPtr<ITfContext> context;
		static CComPtr<ITfFunctionProvider> function_prov;
		static CComPtr<ITextStoreACP> text_store;
		static CComPtr<ITfFnReconversion> reconversion;
		static TfEditCookie cookie;
	};

}
