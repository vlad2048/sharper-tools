import axios, { AxiosResponse } from 'axios';
import { toast } from 'react-toastify';

export class Req {

  static async reqTest(crash: boolean): Promise<number | null> {
    return await this.Extract(() => axios.get('api/reqTest', { params: {
      val: 47,
      crash
    }}));
  }


  private static encodeOpt(s: string | null): string | null {
    if (s === null) return null;
    return this.encode(s);
  }

  private static encode(s: string): string { return encodeURIComponent(s); }
  
  private static async Extract<T>(fun: () => Promise<AxiosResponse<any, any>>): Promise<T | null> {
    try {
      const response = await fun();
      return response.data as T;
    } catch (err) {
      const errMsg = this.GetError(err);
      toast.warn(`API error: ${errMsg}`);
      return null;
    }
  }

  private static GetError(err: any): string {
    if (err.response) {
      // The request was made and the server responded with a status code that falls out of the range of 2xx
      console.log(err.response.data);
      console.log(err.response.status);
      console.log(err.response.headers);
      return err.message + '\n' + err.response.data;
    } else if (err.request) {
      // The request was made but no response was received
      // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
      // http.ClientRequest in node.js
      const requestErr = err.request as XMLHttpRequest;
      console.log(requestErr);
      return err.message + '\n' + requestErr.responseText;
    } else {
      // Something happened in setting up the request that triggered an Error
      console.log('Error', err.message);
      return err.message;
    }
  }
}