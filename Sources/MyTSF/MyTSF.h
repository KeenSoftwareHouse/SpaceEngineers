// MyTSF.h

#pragma once


using namespace System;
namespace MyTSF {

	//������ϊ��p�N���X
	//IME���g���ĕ���������[�}�����炩�Ȍ����蕶�ɕϊ�����.

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
		MyIMEBase(HANDLE hwnd);
		virtual ~MyIMEBase();
		void SetTarget(String^ rTarget);
		String^ PreConvert();
		String^ ConvertDown();
		String^ ConvertUp();
		void SetConvert();
		void ResetConvert();
		Int32 ConvertAbleCount();
		Int32 ConvertTargetAbleCount();
		void MoveConvertTarget(Int32 s);
		Int32 ConvertStartPosition();
		Int32 ConvertEndPosition();
	private:
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
		std::wstring target;
		std::wstring htarget;
		std::vector<std::deque<std::wstring> > outRef;
		size_t target_index;

		CComPtr<ITfThreadMgr> thr_mgr;
		CComPtr<ITfDocumentMgr> doc_mgr;
		CComPtr<ITfContext> context;
		CComPtr<ITfFunctionProvider> function_prov;
		CComPtr<ITextStoreACP> text_store;
		CComPtr<ITfFnReconversion> reconversion;
		TfEditCookie cookie;
	};

}

namespace MyIME
{
	/// <summary>
	/// IME�̊��N���X�ł�
	/// </summary>
	public ref class MyIMEBase
	{
	public:
		/// <summary>
		/// �R���X�g���N�^�ł�.
		/// </summary>
		/// <param name="hwnd">�֘A�t����ꂽ�n���h��</param>
		MyIMEBase(IntPtr hwnd);
		/// <summary>
		/// �R���X�g���N�^�ł�.
		/// </summary>
		/// <param name="hwnd">�֘A�t����ꂽ�n���h��</param>
		MyIMEBase(Int32 hwnd);
		~MyIMEBase();
		/// <summary>
		/// �ϊ��������ݒ肵�܂�.
		/// </summary>
		/// <param name="rTarget">�ϊ�������</param>
		void SetTarget(String^ rTarget);
		/// <summary>
		/// ���{�ꕶ����̓ǂ݂��擾���܂�.
		/// </summary>
		/// <returns>���{�ꕶ����̓ǂ�</returns>
		String^ PreConvert();
		/// <summary>
		/// �ĕϊ����s���������ꂽ���{�ꕶ������擾���܂�.
		/// </summary>
		/// <returns>���{�ꕶ����</returns>
		String^ Convert();
		/// <summary>
		/// �ĕϊ����s���������ꂽ���{�ꕶ������擾���܂�.
		/// </summary>
		/// <returns>���{�ꕶ����</returns>
		String^ ConvertDown();
		/// <summary>
		/// �ĕϊ����s���������ꂽ���{�ꕶ������擾���܂�.
		/// </summary>
		/// <returns>���{�ꕶ����</returns>
		String^ ConvertUp();
		/// <summary>
		/// ���{�ꕶ����𐶐����܂�.
		/// </summary>
		void SetConvert();
		/// <summary>
		/// ��������������������܂�.
		/// </summary>
		void ResetConvert();
		/// <summary>
		/// �ĕϊ��\�ȉ񐔂̑������擾���܂�.
		/// </summary>
		/// <returns>�ĕϊ��\�ȉ�</returns>
		Int32 ConvertAbleCount();
		/// <summary>
		/// �ĕϊ��Ώۂ̍ĕϊ��\�ȉ񐔂��擾���܂�.
		/// </summary>
		/// <returns>�ĕϊ��\�ȉ�</returns>
		Int32 ConvertTargetAbleCount();
		/// <summary>
		/// �ĕϊ�����Ώۂ�ύX���܂�.
		/// </summary>
		/// <param name="s">�V�����Ώۂ̌��̑Ώۂ���̑��ΓI�Ȉʒu</param>
		void MoveConvertTarget(Int32 s);
		/// <summary>
		/// �ĕϊ��Ώۂ̈ʒu���擾���܂�.
		/// </summary>
		/// <returns>�ĕϊ��Ώۂ̊J�n�ʒu</returns>
		Int32 ConvertStartPosition();
		/// <summary>
		/// �ĕϊ��Ώۂ̈ʒu���擾���܂�.
		/// </summary>
		/// <returns>�ĕϊ��Ώۂ̏I���ʒu</returns>
		Int32 ConvertEndPosition();
	private:
		MyTSF::MyIMEBase* um_base;
	};
}

