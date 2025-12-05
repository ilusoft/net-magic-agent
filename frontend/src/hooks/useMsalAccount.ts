import { useEffect, useState } from "react";
import type { AccountInfo } from "@azure/msal-browser";
import { msalInstance } from "@/auth/msalClient";

export function useMsalAccount() {
  const [account, setAccount] = useState<AccountInfo | null>(
    msalInstance.getActiveAccount()
  );

  useEffect(() => {
    const callbackId = msalInstance.addEventCallback((event) => {
      if (
        event.payload &&
        "account" in event.payload &&
        event.payload.account
      ) {
        setAccount(event.payload.account as AccountInfo);
      }
    });

    return () => {
      if (callbackId) {
        msalInstance.removeEventCallback(callbackId);
      }
    };
  }, []);

  return account;
}
