import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';
import { useCallback } from 'react';
import styles from './App.module.scss'
import { Req } from './backend/req';

export const App = () => {

  const onTest = useCallback(async (crash: boolean) => {
    const res = await Req.reqTest(crash);
    if (!res) return;
    console.log('res', res);
  }, []);

  return (
    <>

      <ToastContainer
        position="top-left"
        autoClose={false}
      />

      <div className={styles.App}>
        App
      </div>

      <button onClick={() => onTest(false)}>TestReq No Crash</button>
      <button onClick={() => onTest(true)}>TestReq Crash</button>

    </>
  )
}
