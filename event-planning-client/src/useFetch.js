import { useState, useEffect } from 'react';

const useFetch = (url) => {
  const [data, setData] = useState(null);
  const [isPending, setIsPending] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const token = sessionStorage.getItem("access_token");

    fetch(url, 
        { 
          headers:
          {
            "Accept": "application/json",
            "Authorization": "Bearer " + token
          }
        })
        .then(res => {
          if(res.status === 401)
          {
            sessionStorage.clear();
            throw Error('You are not authorized.');
          }
  
          if (!res.ok) { // error coming back from server
            throw Error('could not fetch the data for that resource');
          }       
          
          return res.json();
        })
        .then(data => {
          setIsPending(false);
          setData(data);
          setError(null);
        })
        .catch(err => {
          console.log(err + err.name)
          if (err.name === 'AbortError') {
            console.log('fetch aborted')
          } else {
            // auto catches network / connection error
            setIsPending(false);
            setError(err.message);
          }
        })
      },
   [url])

  return { data, isPending, error };
}

export default useFetch;