import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
//import tsconfigPaths from 'vite-tsconfig-paths'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react()
    //, tsconfigPaths()
  ],
  server: {
    port: %FrontendPort%,
    proxy: {
      '/api': { target: 'http://localhost:%BackendPort%', ws: true }
    }
  },
  build: {
    manifest: true
  },
  css: {
    preprocessorOptions: {
      scss: { 
        //  @import "${pathSrc}/_base-styles/01-reset.scss";
        //  @import "${nodeSrc}/halfmoon/css/halfmoon-variables.min.css";

         /*additionalData: `
            @import "${pathSrc}/_base-styles/00-mixins.scss";
            @import "${nodeSrc}/tabulator-tables/dist/css/tabulator.min.css";
         `*/
     },
    },
  }
})
