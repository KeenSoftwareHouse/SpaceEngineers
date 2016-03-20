// これは メイン DLL ファイルです。

#include "stdafx.h"
#include "MyTSF.h"

namespace MyTSF
{


#pragma region Managed My TSF Class

	void MyIME::IMEStart(Int32 hwnd)
	{
		MyIMEBase::IMEStart((void*)hwnd);
	}

	void MyIME::IMEStart(IntPtr hwnd)
	{
		MyIMEBase::IMEStart((void*)hwnd);
	}

	void MyIME::IMEEnd()
	{
		MyIMEBase::IMEEnd();
	}

	void MyIME::SetTarget(String^ target)
	{
		MyIMEBase::SetTarget(target);
	}

	void MyIME::SetConvert()
	{
		MyIMEBase::SetConvert();
	}

	String^ MyIME::Convert()
	{
		return MyIMEBase::Convert();
	}

	void MyIME::ResetConvert()
	{
		MyIMEBase::ResetConvert();
	}

	String^ MyIME::PreConvert()
	{
		return MyIMEBase::PreConvert();
	}

	Int32 MyIME::ConvertAbleCount()
	{
		return MyIMEBase::ConvertAbleCount();
	}

	void MyIME::MoveConvertTarget(Int32 s)
	{
		MyIMEBase::MoveConvertTarget(s);
	}

#pragma endregion

#pragma region Unmanaged My TSF Class


	#pragma region Statics

		std::wstring MyIMEBase::target;
		std::wstring MyIMEBase::htarget;
		std::vector<std::queue<std::wstring> > MyIMEBase::outRef;
		size_t MyIMEBase::target_index = 0;

		CComPtr<ITfThreadMgr> MyIMEBase::thr_mgr;
		CComPtr<ITfDocumentMgr > MyIMEBase::doc_mgr;
		CComPtr<ITfContext> MyIMEBase::context;
		CComPtr<ITfFunctionProvider> MyIMEBase::function_prov;
		CComPtr<ITextStoreACP> MyIMEBase::text_store;
		CComPtr<ITfFnReconversion> MyIMEBase::reconversion;
		TfEditCookie MyIMEBase::cookie;

	#pragma endregion

	namespace
	{
		typedef HRESULT(WINAPI *PTF_GETTHREADMGR)(ITfThreadMgr **pptim);

		HRESULT GetThreadMgr(ITfThreadMgr **pptm)
		{
			HRESULT hr = E_FAIL;
			HMODULE hMSCTF = LoadLibrary(TEXT("msctf.dll"));
			ITfThreadMgr *pThreadMgr = NULL;

			if (hMSCTF == NULL)
			{
				//Error loading module -- fail as securely as possible 
			}

			else
			{
				PTF_GETTHREADMGR pfnGetThreadMgr;

				pfnGetThreadMgr = (PTF_GETTHREADMGR)GetProcAddress(hMSCTF, "TF_GetThreadMgr");

				if (pfnGetThreadMgr)
				{
					hr = (*pfnGetThreadMgr)(&pThreadMgr);
				}

				FreeLibrary(hMSCTF);
			}

			//If no object could be obtained, try to create one. 
			if (NULL == pThreadMgr)
			{
				//CoInitialize or OleInitialize must already have been called. 
				hr = CoCreateInstance(CLSID_TF_ThreadMgr,
					NULL,
					CLSCTX_INPROC_SERVER,
					IID_ITfThreadMgr,
					(void**)&pThreadMgr);
			}

			*pptm = pThreadMgr;

			return hr;
		}
	}

	void MyIMEBase::IMEStart(HANDLE hwnd)
	{
		target = L"";
		outRef = std::vector<std::queue<std::wstring> >();

		CoInitialize(NULL);

		HRESULT h;

		h = GetThreadMgr(&thr_mgr);


		if (SUCCEEDED(h))
			h = thr_mgr->CreateDocumentMgr(&doc_mgr);

		TfClientId cid = 0;

		if (SUCCEEDED(h))
			h = thr_mgr->Activate(&cid);


		if (SUCCEEDED(h))
			text_store = static_cast < ITextStoreACP*> (new MyTextStore());
		
		if (SUCCEEDED(h))
			h = doc_mgr->CreateContext(
				cid, 0, (ITextStoreACP*)(text_store),
				&context, &cookie);

		if (SUCCEEDED(h))
			h = doc_mgr->Push(context);

		if (SUCCEEDED(h))
			h = thr_mgr->GetFunctionProvider(GUID_SYSTEM_FUNCTIONPROVIDER, &function_prov);

		if (SUCCEEDED(h))
			h = function_prov->GetFunction(GUID_NULL, IID_ITfFnReconversion, (IUnknown**)&reconversion);

		if (SUCCEEDED(h))
			h = thr_mgr->SetFocus(doc_mgr);

		if (FAILED(h))
			IMEEnd();
	}

	void MyIMEBase::IMEEnd()
	{

		MyIMEBase::RELEASE(text_store);
		MyIMEBase::RELEASE(reconversion);
		MyIMEBase::RELEASE(function_prov);
		MyIMEBase::RELEASE(context);

		if (doc_mgr)
		{
			doc_mgr->Pop(TF_POPF_ALL);
			RELEASE(doc_mgr);
		}

		if (thr_mgr)
		{
			thr_mgr->Deactivate();
			RELEASE(thr_mgr);
		}

	}

	void MyIMEBase::SetTarget(String^ target)
	{
		MyIMEBase::target = msclr::interop::marshal_as<std::wstring>(target);
		MyIMEBase::htarget = msclr::interop::marshal_as<std::wstring>(Microsoft::International::Converters::KanaConverter::RomajiToHiragana(target));
	}

	String^ MyIMEBase::PreConvert()
	{
		std::wstring tans = htarget;
		String^ ans = gcnew String(tans.c_str());
		return ans;
	}

	void MyIMEBase::SetConvert()
	{
		outRef = std::vector<std::queue<std::wstring> >();
		target_index = -1;

		HRESULT h = E_FAIL;
		//std::vector<TF_SELECTION> selections((size_t)sel_len);
		size_t start = 0, tcount = 0;
		size_t hts = htarget.size();

		MyTextStore* store = dynamic_cast<MyTextStore*>((ITextStoreACP*)text_store);
		if (store == nullptr)
			return;

		std::queue<std::wstring> bq,bbq;

		while (start + tcount <= hts)
		{
			size_t sel_len = 1;
			TF_SELECTION selections = { 0 };
			ULONG ferched_count = 0;
			ITfRange* rrange  = NULL;
			BOOL convert_flg = FALSE;
			ITfCandidateList* list = NULL;
			ULONG length = 0;
			do
			{
				bbq = bq;
				bq = std::queue<std::wstring>();

				tcount++;
				store->SetString(htarget.substr(start, tcount));

				if (store->SetLock(TS_LF_READ))
				{
					ITfContext* bcontext = static_cast<ITfContext*>(context);
					h = bcontext->GetSelection(
						cookie, TF_DEFAULT_SELECTION, sel_len,
						&selections, &ferched_count);

					store->ResetLock();

					if (FAILED(h))
						return;
				}

				h = reconversion->QueryRange(selections.range, &rrange, &convert_flg);
				if (FAILED(h) || rrange == NULL)
					return;

				h = reconversion->GetReconversion(selections.range, &list);
				if (FAILED(h) || list == NULL)
					return;

				h = list->GetCandidateNum(&length);
				if (FAILED(h))
					return;

				ITfCandidateString* pstring = NULL;
				BSTR bstr = NULL;


				for (ULONG i = 0; i < length; i++)
				{
					ITfCandidateString* pstring = NULL;
					BSTR bstr = NULL;
					if (SUCCEEDED(list->GetCandidate(i, &pstring)) &&
						SUCCEEDED(pstring->GetString(&bstr)))
					{
						std::wstring buf(bstr);
						bq.push(buf);
					}
				}
			} while (bq != bbq && start + tcount <= hts);

			outRef.push_back(bq);
			start += tcount - 1;
			tcount = 0;

			RELEASE(list);
			RELEASE(rrange);
			RELEASE(selections.range);
		}

	}

	String^ MyIMEBase::Convert()
	{

		std::wstring tans = L"";
		for (int i = 0; i < outRef.size(); i++)
		{
			if (!outRef[i].empty())
			{
				if (i == target_index)
				{
					std::wstring buf = outRef[i].front();
					outRef[i].pop();
					outRef[i].push(buf);
				}
				else if(target_index == -1)
					target_index = 0;
				std::wstring buf = outRef[i].front();
				tans += buf;
			}
		}
		String^ ans = gcnew String(tans.c_str());
		return ans;
	}

	void MyIMEBase::ResetConvert()
	{}

	Int32 MyIMEBase::ConvertAbleCount()
	{
		size_t ans = 1;
		for (int i = 0; i < outRef.size(); i++)
		{
			ans *= outRef[i].size();
		}
		return ans;
	}

	void MyIMEBase::MoveConvertTarget(Int32 s)
	{
		if(target_index <= 0)
			target_index = 0;
		target_index += s;
		if (target_index <= 0)
			target_index = 0;
		else if (target_index >= outRef.size())
			target_index = outRef.size() - 1;
	}

#pragma endregion

#pragma region Text Store Contents

	MyTextStore::MyTextStore()
		:now_ref_counter(0),ref_flgs(0),all_ref_count(1),
		rsink(NULL),mask(0),text()
	{}

	MyTextStore::~MyTextStore()
	{
	}

	bool MyTextStore::IsLock(DWORD iFlg)
	{
		return (ref_flgs & iFlg) == iFlg;
	}

	bool MyTextStore::SetLock(DWORD iFlgs)
	{
		if (now_ref_counter == 0)
		{
			ref_flgs = iFlgs;
			InterlockedIncrement(&now_ref_counter);
			return true;
		}
		return false;
	}

	bool MyTextStore::ResetLock()
	{
		if (ref_flgs)
		{
			ref_flgs = 0;
			InterlockedDecrement(&now_ref_counter);
			return true;
		}
		return false;
	}

	bool MyTextStore::SetString(std::wstring istring)
	{
		if (!SetLock(TS_LF_READWRITE))
			return false;

		TS_TEXTCHANGE tc = { 0 };
		tc.acpStart = 0;
		tc.acpOldEnd = text.size();
		tc.acpNewEnd = istring.size();

		text = istring;
		ResetLock();

		if (rsink && (mask & TS_AS_TEXT_CHANGE))
			rsink->OnTextChange(0, &tc);

		return true;
	}

#pragma region Regular Use Methods

	ULONG STDMETHODCALLTYPE MyTextStore::AddRef()
	{
		all_ref_count++;
		return all_ref_count;
	}

	ULONG STDMETHODCALLTYPE MyTextStore::Release()
	{
		all_ref_count--;
		if (all_ref_count == 0)
			delete this;

		return all_ref_count;
	}

	HRESULT STDMETHODCALLTYPE MyTextStore::QueryInterface(REFIID riid, void **ppvObject)
	{
		if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_ITextStoreACP))
		{
			*ppvObject = static_cast<ITextStoreACP*>(this);
			AddRef();
			return S_OK;
		}
		else
		{
			*ppvObject = NULL;
			return E_NOINTERFACE;
		}
	}
	
	HRESULT STDMETHODCALLTYPE MyTextStore::AdviseSink(REFIID riid, IUnknown* punk, DWORD dwMask)
	{
		if(IsBadReadPtr(punk,sizeof(IUnknown*) ||!IsEqualIID(riid,IID_ITextStoreACPSink)))
			return E_FAIL;

		if (punk == rsink)
		{
			mask = dwMask;
			return S_OK;
		}
		else if (rsink != NULL)
			return CONNECT_E_ADVISELIMIT;
		else
		{
			HRESULT h = punk->QueryInterface(IID_ITextStoreACPSink, (void**)&rsink);
			if (SUCCEEDED(h))
				mask = dwMask;
			return h;
		}

		return E_FAIL;
	}

	
	HRESULT STDMETHODCALLTYPE MyTextStore::UnadviseSink(IUnknown* punk)
	{
		if (IsBadReadPtr(punk, sizeof(void*)))
			return E_INVALIDARG;

		if (punk != rsink)
			return CONNECT_E_NOCONNECTION;

		rsink->Release();
		mask = 0;
		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE MyTextStore::RequestLock(DWORD dwLockFlags, HRESULT* phrSession)
	{
		if (rsink == NULL)
		{
			return E_UNEXPECTED;
		}

		if (IsBadWritePtr(phrSession, sizeof(HRESULT)))
		{
			return E_INVALIDARG;
		}

		*phrSession = E_FAIL;

		if (!SetLock(dwLockFlags))
		{
			if ((dwLockFlags & TS_LF_SYNC) == TS_LF_SYNC)
				*phrSession = TS_E_SYNCHRONOUS;
			else
				*phrSession = E_NOTIMPL;
		}
		else
		{
			try
			{
				*phrSession = rsink->OnLockGranted(dwLockFlags);
			}
			catch (...)
			{
				*phrSession = E_FAIL;
			}
			ResetLock();
		}
		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE MyTextStore::GetStatus(TS_STATUS* pdcs)
	{
		if (IsBadWritePtr(pdcs, sizeof(TS_STATUS)))
			return E_INVALIDARG;

		pdcs->dwDynamicFlags = TS_SD_READONLY;
		pdcs->dwStaticFlags = TS_SS_REGIONS;
		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE MyTextStore::GetSelection(
		ULONG ulIndex, ULONG ulCount, TS_SELECTION_ACP* pSelection, 
		ULONG* pcFetched)
	{
		if (IsBadWritePtr(pSelection, sizeof(TS_SELECTION_ACP) || 
			IsBadWritePtr(pcFetched, sizeof(ULONG))))
			return E_INVALIDARG;

		*pcFetched = 0;

		if (!IsLock(TS_LF_READ))
			return TS_E_NOLOCK;

		if (ulIndex != TF_DEFAULT_SELECTION && ulIndex > 0)
			return E_INVALIDARG;

		memset(pSelection, 0, sizeof(pSelection[0]));
		pSelection[0].acpStart = 0;
		pSelection[0].acpEnd = text.size();
		pSelection[0].style.fInterimChar = FALSE;
		pSelection[0].style.ase = TS_AE_START;

		*pcFetched = 1;
		
		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE MyTextStore::GetText(
		LONG acpStart, LONG acpEnd, PWSTR pchPlain, 
		ULONG cchPlainReq, ULONG* pcchPlainOut, TS_RUNINFO* prgRunInfo, 
		ULONG ulRunInfoReq, ULONG* pulRunInfoOut, LONG* pacpNext)
	{
		if (!IsLock(TS_LF_READ))
			return TS_E_NOLOCK;

		ULONG textlen = text.size();
		ULONG copylen = min(textlen, cchPlainReq);

		if (IsBadWritePtr(pchPlain, cchPlainReq * sizeof(wchar_t)) == FALSE)
		{
			memset(pchPlain, 0, cchPlainReq * sizeof(wchar_t));
			memcpy(pchPlain, text.c_str(), copylen * sizeof(wchar_t));
		}

		if (IsBadWritePtr(pcchPlainOut, sizeof(ULONG)) == FALSE)
			*pcchPlainOut = copylen;


		if (IsBadWritePtr(prgRunInfo, sizeof(TS_RUNINFO)) == FALSE)
		{
			prgRunInfo[0].type = TS_RT_PLAIN;
			prgRunInfo[0].uCount = textlen;
		}

		if (IsBadWritePtr(pulRunInfoOut, sizeof(ULONG)) == FALSE)
			*pulRunInfoOut = 1;

		if (IsBadWritePtr(pacpNext, sizeof(LONG)) == FALSE)
			*pacpNext = acpStart + textlen;

		return S_OK;
	}

	HRESULT STDMETHODCALLTYPE MyTextStore::QueryInsert(
		LONG acpTestStart, LONG acpTestEnd, ULONG cch, 
		LONG* pacpResultStart, LONG* pacpResultEnd)
	{
		if (acpTestStart < 0 || acpTestStart > acpTestEnd ||
			acpTestEnd > text.size())
			return E_INVALIDARG;
		else
		{
			if (pacpResultStart != NULL)
				*pacpResultStart = acpTestStart;
			
			if (pacpResultEnd != NULL)
				*pacpResultEnd = acpTestEnd;

			return S_OK;
		}
	}

#pragma endregion

#pragma endregion

}