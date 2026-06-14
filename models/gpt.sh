~/llama.cpp/build/bin/llama-server \
   -m ./gpt-oss-20b-MXFP4.gguf \
   --host 0.0.0.0 \
   --port 8080 \
   -ngl 8 \
   -c 16384 \
   --jinja \
   --verbosity 3 \
   --chat-template-kwargs '{"reasoning_effort": "medium"}'
